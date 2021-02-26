using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scs.BasicManagement.Application.Manager;
using Scs.BasicManagement.Application.ViewModels.Role;

namespace Scs.BasicManagement.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleManager _roleManager;

        public RoleController(IRoleManager roleManager)
        {
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDetailViewModel>> GetUserById(Guid id)
        {
            var user = await _roleManager.GetByIdAsync(id);
            return Ok(user);
        }

        [HttpGet]
        public async Task<ActionResult<RoleListViewModel>> GetAll()
        {
            var users = await _roleManager.GetAllAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<RoleEditViewModel>> Create([FromForm] RoleEditViewModel userEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _roleManager.CreateAsync(userEditViewModel);
                return Ok(userEditViewModel);
            }
            else
            {
                return UnprocessableEntity(userEditViewModel);
            }
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromForm] RoleEditViewModel userEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _roleManager.UpdateAsync(userEditViewModel);
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
            await _roleManager.DeleteAsync(id);
            return NoContent();
        }
    }
}
