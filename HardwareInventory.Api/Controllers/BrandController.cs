using Dapper;
using HardwareInventory.Api.Attributes;
using HardwareInventory.Api.Database;
using HardwareInventoryLibrary.Brand;
using HardwareInventoryLibrary.Ram;
using Mapster;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using TruSql.Builder;
using TruSql.Core.QueryBuilder;
using TruSql.Interface;

namespace HardwareInventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandController : Controller
    {
        private readonly AppDbContext _db;
        private readonly DbConnectionFactory _factory;
        private readonly ILogger<BrandController> _logger;
        private readonly IQueryFilter _query;
        private readonly SQLQueryResult _builtBaseQuery;
        private readonly HashSet<string> _ruleSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public BrandController(AppDbContext db, DbConnectionFactory factory, ILogger<BrandController> logger)
        {
            _db = db;
            _factory = factory;
            _logger = logger;
            foreach (var prop in typeof(BrandDatabaseModel).GetProperties())
            {
                if (prop.GetCustomAttribute<PatchBlockedAttribute>() != null) continue;
                _ruleSet.Add($"/{prop.Name}");
            }

            _query = QueryBuilder
                .Create<BrandReadModel>()
                .From<BrandDatabaseModel>("b")
                .Select("b.*")
                .ValidateBaseQuery();

            _builtBaseQuery = _query.Build();
        }
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BrandReadModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BrandReadModel>>> Get(
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

                var brands = await conn.QueryAsync<BrandReadModel>(commandDefinition);

                return Ok(brands);
            }
            catch (SyntaxErrorException ex)
            {
                return BadRequest(new { message = "Invalid query arguments provided.", detail = ex.Message });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to fetch Brand records. Where: {Where}; OrderBy: {OrderBy}",
                    where,
                    orderBy);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An unexpected error occurred while fetching hardware records."
                });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BrandReadModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var sql = $"{_builtBaseQuery.RawSQL} Where b.Id = @Id";

                using var conn = _factory.Create();

                var commandDefinition = new CommandDefinition(
                    sql,
                    new { Id = id },
                    cancellationToken: cancellationToken);

                var brand = await conn.QueryFirstOrDefaultAsync<BrandReadModel>(commandDefinition);

                if (brand == null)
                {
                    return NotFound(new { message = $"Brand record with ID {id} was not found." });
                }

                return Ok(brand);
            }
            catch (SyntaxErrorException ex)
            {
                return BadRequest(new { message = "Invalid query arguments provided.", detail = ex.Message });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to fetch Brand records. id: {id}",
                    id);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An unexpected error occurred while fetching hardware records."
                });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add(
            [FromBody] BrandCreateModel brand,
            CancellationToken cancellationToken)
        {
            if (brand == null) return BadRequest("Invalid data.");

            BrandDatabaseModel newBrand;

            try
            {
                newBrand = brand.Adapt<BrandDatabaseModel>();
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "One or more supplied values are invalid." });
            }

            _db.BrandDatas.Add(newBrand);
            await _db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = newBrand.Id }, newBrand);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(
            int id, 
            [FromBody] BrandCreateModel incomingData,
            CancellationToken cancellationToken)
        {
            if (incomingData == null) return BadRequest("Invalid data.");

            var existingBrand = await _db.BrandDatas.FindAsync([id], cancellationToken);
            if (existingBrand == null) return NotFound($"Brand with the id {id} doesn't exist.");

            BrandDatabaseModel replacement;

            try
            {
                replacement = incomingData.Adapt<BrandDatabaseModel>();
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "One or more supplied values are invalid." });
            }

            replacement.Id = existingBrand.Id;

            _db.Entry(existingBrand).CurrentValues.SetValues(replacement);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id}")]
        [Consumes("application/json-patch+json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Patch(
            int id, 
            [FromBody] JsonPatchDocument incomingData,
            CancellationToken cancellationToken)
        {
            if (incomingData == null) return BadRequest("Invalid data");
            if (incomingData.Operations.Count == 0 ||
                incomingData.Operations.Any(o =>
                o.OperationType != OperationType.Replace ||
                !_ruleSet.Contains(o.path)))
            {
                return BadRequest("Invalid Patch operations.");
            }

            var existingBrand = await _db.BrandDatas.FindAsync([id], cancellationToken);
            if (existingBrand == null) return NotFound($"Brand with ID \"{id}\" doesn't exist.");

            var patchedBrand = existingBrand.Adapt<BrandDatabaseModel>();

            try
            {
                incomingData.ApplyTo(patchedBrand);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = "One or more supplied values are invalid." });
            }

            _db.Entry(existingBrand).CurrentValues.SetValues(patchedBrand);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(existingBrand);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var existingBrand = await _db.BrandDatas.FindAsync([id], cancellationToken);
            if (existingBrand == null)
                return NotFound($"Brand with ID \"{id}\" doesn't exist.");

            _db.BrandDatas.Remove(existingBrand);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
