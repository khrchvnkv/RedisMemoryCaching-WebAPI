using CachingWithRedis_WebAPI.Data;
using CachingWithRedis_WebAPI.Models;
using CachingWithRedis_WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CachingWithRedis_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DriversController : ControllerBase
    {
        private const string DriverKeyFormat = "driver_{0}";
        private const string DriversKey = "driver";
        
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DriversController> _logger;

        public DriversController(ICacheService cacheService, AppDbContext dbContext, ILogger<DriversController> logger)
        {
            _cacheService = cacheService;
            _dbContext = dbContext;
            _logger = logger; 
        }

        [HttpGet("get-driver")]
        public async Task<IActionResult> GetDriver([FromQuery] int id)
        {
            var cachedDriver = _cacheService.GetData<Driver>(string.Format(DriverKeyFormat, id));
            if (cachedDriver is not null) return Ok(cachedDriver);
            
            var drivers = await GetDriversFromCacheOrDatabase();
            var driver = drivers.ToList().FirstOrDefault(d => d.Id == id);
            if (driver is not null) return Ok(driver);

            return NotFound(id);
        }

        [HttpGet("get-drivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await GetDriversFromCacheOrDatabase();
            return Ok(drivers);
        }

        [HttpPost("add-driver")]
        public async Task<IActionResult> AddDriver(Driver driver)
        {
            var addedDriver = await _dbContext.Drivers.AddAsync(driver);
            _cacheService.SetData($"driver_{addedDriver.Entity.Id}", addedDriver.Entity,
                GetExpirationDateTime());
            await _dbContext.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetDriver), new { driver.Id }, driver);
        }

        [HttpDelete("delete-driver")]
        public async Task<IActionResult> DeleteDriver([FromQuery] int id)
        {
            var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver is not null)
            {
                _dbContext.Drivers.Remove(driver);
                _cacheService.RemoveData(string.Format(DriverKeyFormat, id));
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }

            return NotFound(id);
        }

        private async Task<IEnumerable<Driver>> GetDriversFromCacheOrDatabase()
        {
            var cachedDriversCollection = _cacheService.GetData<IEnumerable<Driver>>(DriversKey);
            if (cachedDriversCollection is not null && cachedDriversCollection.Any()) return cachedDriversCollection;

            var dbDrivers = await _dbContext.Drivers.ToListAsync();
            if (dbDrivers.Any()) _cacheService.SetData(DriversKey, dbDrivers, GetExpirationDateTime());
            return dbDrivers;
        }
        private DateTimeOffset GetExpirationDateTime() => DateTimeOffset.Now.AddSeconds(30);
    }
}