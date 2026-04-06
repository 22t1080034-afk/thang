using SV22T1080034.DomainModels.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1080034.DataLayers.Interfaces
{
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        Task<bool> ValidateEmailAsync(string email, int supplierID = 0);
    }
}
