using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyApp.DataAccess;
using SampleApp.Repository;

namespace SampleApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private IConfiguration _configuration;
        private UserApiDal _userapidal;

        public UserApiController(IConfiguration configuration, UserApiDal userapidal)
        {
            _configuration = configuration;
            _userapidal = userapidal;
            
        }


        [HttpGet("getbyemail")]
        public IActionResult GetUserByEmail(string email)
        {
            var user = _userapidal.GetUserByEmail(email);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("getuserswithGId")]
        public IActionResult GetUsersWithGId()
        {
                var user = _userapidal.GetUsersWithGoogleId();

                if (user == null)
                    return NotFound();

                return Ok(user);
        }
    }
}
