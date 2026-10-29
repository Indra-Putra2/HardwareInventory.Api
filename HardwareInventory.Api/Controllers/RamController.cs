using Dapper;
using HardwareInventory.Api.Attributes;
using HardwareInventory.Api.Database;
using HardwareInventoryLibrary.Brand;
using HardwareInventoryLibrary.Ram;
using Mapster;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;
using TruSql.Builder;
using TruSql.Core.QueryBuilder;
using TruSql.Interface;

namespace HardwareInventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RamController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly DbConnectionFactory _factory;
        private readonly IQueryFilter _query;
        private readonly SQLQueryResult _baseQueryBuilt;
        private readonly HashSet<string> _patchRule = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<RamController> _logger;
        public 
            RamController(AppDbContext db, 
            DbConnectionFactory factory,
            ILogger<RamController> logger)
        {
            _db = db;
            _factory = factory;
            _logger = logger;

            foreach (var prop in typeof(RamUpdateModel).GetProperties())
            {
                if (prop.GetCustomAttribute<PatchBlockedAttribute>() != null) continue;
                _patchRule.Add($"/{prop.Name}");
            }

            _query = QueryBuilder
                .Create<RamReadModel>()
                .From<RamDatabaseModel>("r")
                .Select("r.*, b.name as brandname")
                .Join<BrandDatabaseModel>("b", "b.id = r.brandid")
                .ValidateBaseQuery();

            _baseQueryBuilt = _query.Build();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RamReadModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RamReadModel>>> Get(
            [FromQuery] string? where,
            [FromQuery] string? orderBy,
            CancellationToken cancellationToken,
            [FromQuery] int limit = 1)
        {
            const int maxLimit = 100;
            int safeLimit = Math.Clamp(limit <= 0 ? 1 : limit, 1, maxLimit);

            try
            {
                var sql = _query
                    .Where(where)
                    .OrderBy(orderBy)
                    .Limit(safeLimit)
                    .Build();

                var parameters = new DynamicParameters();
                foreach (var param in sql.Parameters)
                {
                    parameters.Add(param.Key, param.Value.Value);
                }

                using var conn = _factory.Create();

                var commandDefinition = new CommandDefinition(
                    sql.ParameterizedSQL,
                    parameters,
                    cancellationToken: cancellationToken);

                var rams = await conn.QueryAsync<RamReadModel>(commandDefinition);

                return Ok(rams);
            }
            catch (SyntaxErrorException ex)
            {
                return BadRequest(new { message = "Invalid query arguments provided.", detail = ex.Message });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to fetch RAM records. Where: {Where}; OrderBy: {OrderBy}",
                    where,
                    orderBy);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An unexpected error occurred while fetching hardware records."
                });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RamReadModel>> GetById(
            int id, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var conn = _factory.Create();

                var sql = $"{_baseQueryBuilt.RawSQL} WHERE r.Id = @Id";

                var commandDefinition = new CommandDefinition(
                    sql,
                    new { Id = id },
                    cancellationToken: cancellationToken);

                var ram = await conn.QueryFirstOrDefaultAsync<RamReadModel>(commandDefinition);

                if (ram == null)
                {
                    return NotFound(new { message = $"RAM record with ID {id} was not found." });
                }

                return Ok(ram);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to fetch RAM record {Id}", id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred while fetching hardware records." });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add(
            [FromBody] RamCreateModel incomingData, 
            CancellationToken cancellationToken)
        {
            if (incomingData == null)
                return BadRequest(new { message = "Invalid Data." });

            if (!await BrandExist(incomingData.BrandId, cancellationToken)) 
                return BadRequest(new { message = "The specified brand does not exist." });

            RamDatabaseModel newRam;

            try
            {
                newRam = incomingData.Adapt<RamDatabaseModel>();
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    message = "One or more supplied values are invalid."
                });
            }

            _db.RamDatas.Add(newRam);
            await _db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = newRam.Id }, newRam);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(
            int id, 
            [FromBody] RamCreateModel incomingData, 
            CancellationToken cancellationToken)
        {
            if(incomingData == null)
                return BadRequest(new { message = "Invalid data." });

            if (!await BrandExist(incomingData.BrandId, cancellationToken))
                return BadRequest(new { message = "The specified brand does not exist." });

            var existingRam = await _db.RamDatas.FindAsync([id], cancellationToken);

            if (existingRam == null)
                return NotFound(new { message = $"Ram with the id \"{id}\" doesn't exist." });

            RamDatabaseModel replacement;

            try
            {
                replacement = incomingData.Adapt<RamDatabaseModel>();
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    message = "One or more supplied values are invalid."
                });
            }

            replacement.Id = existingRam.Id;

            _db.Entry(existingRam).CurrentValues.SetValues(replacement);

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPatch("{id}")]
        [Consumes("application/json-patch+json")]
        [ProducesResponseType(typeof(RamDatabaseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Patch(
            int id,
            [FromBody] JsonPatchDocument incomingData,
            CancellationToken cancellationToken)
        {
            if (incomingData == null) return BadRequest();
            if (incomingData.Operations.Count == 0 ||
                incomingData.Operations.Any(o =>
                    o.OperationType != OperationType.Replace ||
                    !_patchRule.Contains(o.path)))
            {
                return BadRequest(new { message = "Invalid patch operations." });
            }

            var brandId = incomingData.Operations.FirstOrDefault(o => o.path.Equals("/brandid", StringComparison.InvariantCultureIgnoreCase));

            if (brandId != null)
            {
                if (brandId.value == null ||
                    !int.TryParse(brandId.value.ToString(), out int bId))
                {
                    return BadRequest(new { message = "Invalid BrandId." });
                }

                if (!await BrandExist(bId, cancellationToken))
                {
                    return BadRequest(new { message = "The specified brand does not exist." });
                }
            }

            var existingRam = await _db.RamDatas.FindAsync([id], cancellationToken);
            if (existingRam == null) return NotFound();

            var patchedRam = existingRam.Adapt<RamDatabaseModel>();

            try
            {
                incomingData.ApplyTo(patchedRam);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = "One or more supplied values are invalid." });
            }

            _db.Entry(existingRam).CurrentValues.SetValues(patchedRam);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(existingRam);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(
            int id, 
            CancellationToken cancellationToken)
        {
            var existingRam = await _db.RamDatas.FindAsync([id], cancellationToken);
            if (existingRam == null)
                return NotFound(new { message = $"Ram with the id \"{id}\" doesn't exist." });

            _db.RamDatas.Remove(existingRam);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task<bool> BrandExist(int brandId, CancellationToken cancellationToken)
        {
            return await _db.BrandDatas.AnyAsync(b => b.Id == brandId, cancellationToken);
        }
    }
}
