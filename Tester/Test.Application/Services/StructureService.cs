using System;
using Girvs.Application;
using Girvs.Domain.Caching.Interface;
using Test.Domain.Managers;
using Test.Domain.Models;
using Test.GrpcService.BaseServices.StructureGrpcService;

namespace Test.Application.Services
{
    public class StructureService : StructureGrpcService.StructureGrpcServiceBase, IGrpcService
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<Structure> _cacheKeyManager;
        private readonly IStructureManager _structureManager;

        public StructureService(IStaticCacheManager staticCacheManager,ICacheKeyManager<Structure> cacheKeyManager,IStructureManager structureManager)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _structureManager = structureManager ?? throw new ArgumentNullException(nameof(structureManager));
        }

        //public override async Task<GetStructureResponse> GetStructure(Empty request, ServerCallContext context)
        //{
        //    return await _staticCacheManager.GetAsync(_cacheKeyManager.CacheKeyListAllPrefix ,async () =>
        //    {
        //        var list = await _structureManager.GetAllListAsync();
        //        GetStructureResponse structureResponse = new GetStructureResponse();

        //        foreach (var structureDto in list)
        //        {
        //            var message = new StructureMessage()
        //            {
        //                Id = structureDto.Id.ToString(),
        //                Action = structureDto.Action,
        //                Controller = structureDto.Controller,
        //                ImageUrl = structureDto.ImageUrl,
        //                IsIdentification = structureDto.IsIdentification,
        //                IsPublic = structureDto.IsPublic,
        //                Name = structureDto.Name,
        //                ParentID = structureDto.ParentID.ToString(),
        //                PermissionDescString = structureDto.PermissionDescString
        //            };
        //            message.Permissions.AddRange(BuildPermission(structureDto.Permissions));
        //        }

        //        return structureResponse;
        //    });
        //}


        //List<Cmmp.GrpcService.BaseServices.Public.Permission> BuildPermission(List<Cmmp.Domain.Enumerations.Permission> ps)
        //{
        //    List<Cmmp.GrpcService.BaseServices.Public.Permission> permissions =
        //        new List<GrpcService.BaseServices.Public.Permission>();
        //    foreach (var permission in ps)
        //    {
        //        permissions.Add((Cmmp.GrpcService.BaseServices.Public.Permission)(int)permission);
        //    }

        //    return permissions;
        //}
    }
}