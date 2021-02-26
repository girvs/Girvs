using Microsoft.AspNetCore.Mvc;
using Scs.BasicManagement.Application.Manager;
using Scs.BasicManagement.Application.ViewModels.User;
using System;
using System.Threading.Tasks;

namespace Scs.BasicManagement.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserManager _userManager;

        public UserController(IUserManager userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDetailViewModel>> GetUserById(Guid id)
        {
            var user = await _userManager.GetByIdAsync(id);
            return Ok(user);
        }

        [HttpGet("Query")]
        public async Task<ActionResult<UserQueryViewModel>> GetUserByQuery([FromQuery] UserQueryViewModel queryViewModel)
        {
            var users = await _userManager.GetUserByQueryAsync(queryViewModel);
            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<UserEditViewModel>> Create([FromForm] UserEditViewModel userEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _userManager.CreateAsync(userEditViewModel);
                return Ok(userEditViewModel);
            }
            else
            {
                return UnprocessableEntity(userEditViewModel);
            }
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromForm] UserEditViewModel userEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _userManager.UpdateAsync(userEditViewModel);
                return NoContent();
            }
            else
            {
                return UnprocessableEntity(userEditViewModel);
            }
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _userManager.DeleteAsync(id);
            return NoContent();
        }
    }
}
