using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scs.BasicManagement.Application.Manager;
using Scs.BasicManagement.Application.ViewModels.Permission;

namespace Scs.BasicManagement.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionManager _permissionManager;

        public PermissionController(IPermissionManager permissionManager)
        {
            _permissionManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
        }

        [HttpGet("ByCurrentUser")]
        public async Task<ActionResult<List<PermissionByCurrentUserViewModel>>> GetCurrentUserPermissionAsync()
        {
            var ps = await _permissionManager.GetCurrentUserPermissionAsync();
            return Ok(ps);
        }

[HttpGet("BySet")]
        public async Task<ActionResult<List<PermissionDetailViewModel>>> GetPermissionAsync(
            [FromQuery] PermissionQueryViewModel queryViewModel)
        {
            var ps = await _permissionManager.GetPermissionAsync(queryViewModel);
            return Ok(ps);
        }

        [HttpPost]
        public async Task<ActionResult> SavePermissionAsync([FromForm] SavePermisssionEditViewModel saveViewModel)
        {
            await _permissionManager.SavePermissionAsync(saveViewModel);
            return NoContent();
        }
    }
}