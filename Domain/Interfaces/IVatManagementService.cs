using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces;

public interface IVatManagementService
{
    Task<Vat> AddVatAsync(Vat vat);

    Task<Vat> DeleteVatAsync(Guid id);

    Task<Vat> GetVatAsync(Guid id);

    Task<List<Vat>> GetVatListAsync();

    Task<bool> VatExistsAsync(Guid id);

    Task<Vat> UpdateVatAsync(Vat vat);
}