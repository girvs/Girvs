namespace Girvs.WebGrpcFrameWork.AutoMapping
{
    // public class GrpcAutoMappingProfile : Profile, IOrderedMapperProfile
    // {
    //     public GrpcAutoMappingProfile()
    //     {
    //         CreateGrpcAutoMappingProfile();
    //     }
    //
    //     private void CreateGrpcAutoMappingProfile()
    //     {
    //         var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
    //         var modelTypes = typeFinder.FindClassesOfType<IMessage>(true, false);
    //         CreateModelMapping(modelTypes);
    //     }
    //
    //     private void CreateModelMapping(IEnumerable<Type> types)
    //     {
    //         foreach (var modelType in types)
    //         {
    //             if (Attribute.GetCustomAttribute(modelType, typeof(AutoMapFromAttribute)) is AutoMapFromAttribute fromEntity)
    //             {
    //                 CreateMap(fromEntity.EntityType, modelType);
    //             }
    //
    //             if (Attribute.GetCustomAttribute(modelType, typeof(AutoMapToAttribute)) is AutoMapToAttribute toEntity)
    //             {
    //                 CreateMap(modelType, toEntity.EntityType);
    //             }
    //         }
    //     }
    //
    //     public int Order { get; } = 100;
    // }
}