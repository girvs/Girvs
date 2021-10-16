using System;
using System.Collections.Generic;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    public class BatchChangeUserStateViewModel
    {
        public DataState DataState { get; set; }
        public IList<Guid> Ids { get; set; }
    }
}