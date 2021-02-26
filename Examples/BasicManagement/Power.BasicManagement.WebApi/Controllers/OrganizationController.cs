using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scs.BasicManagement.Application.Manager;
using Scs.BasicManagement.Application.ViewModels.Organization;

namespace Scs.BasicManagement.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationManager _organizationManager;

        [HttpGet]
        public async Task<ActionResult<List<OrganizationListViewModel>>> GetAll()
        {
            var users = await _organizationManager.GetAllAsync();
            return Ok(users);
        }

        public OrganizationController(IOrganizationManager organizationManager)
        {
            _organizationManager = organizationManager ?? throw new ArgumentNullException(nameof(organizationManager));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationDetailViewModel>> GetUserById(Guid id)
        {
            var o = await _organizationManager.GetByIdAsync(id);
            return Ok(o);
        }


        [HttpPost]
        public async Task<ActionResult<OrganizationEditViewModel>> Create([FromForm] OrganizationEditViewModel organizationEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _organizationManager.CreateAsync(organizationEditViewModel);
                return Ok(organizationEditViewModel);
            }
            else
            {
                return UnprocessableEntity(organizationEditViewModel);
            }
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromForm] OrganizationEditViewModel organizationEditViewModel)
        {
            if (ModelState.IsValid)
            {
                await _organizationManager.UpdateAsync(organizationEditViewModel);
                return NoContent();
            }
            else
            {
                return UnprocessableEntity(organizationEditViewModel);
            }
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _organizationManager.DeleteAsync(id);
            return NoContent();
        }
    }
}
