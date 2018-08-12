using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly IHttpContextAccessor _httpContextAccessor; 
  
        public ServicesController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)  
        {  
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;  
        } 

        // GET api/services
        [HttpGet]
        public ActionResult<string> Get()
        {
            var ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            return $"ip={ip}  Host: {Environment.MachineName}\nSERVICE_NAME={_configuration["SERVICE_NAME"]}";
        }

        // GET api/services/{id}
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return $"service={id}";
        }

        // POST api/services
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/services/{id}
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/services/{id}
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
