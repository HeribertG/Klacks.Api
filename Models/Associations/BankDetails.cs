using System.ComponentModel.DataAnnotations;
using Klacks.Api.Datas;

namespace Klacks.Api.Models.Associations;

public class BankDetails : BaseEntity
{
    public string AccountDescription { get; set; } = "";

    public string AccountNumber { get; set; } = "";
    public string SubscriberNumber { get; set; } = "";

    public int Position { get; set; }

    [StringLength(50)]
    public string Company { get; set; } = "";

    [StringLength(15)]
    public string Title { get; set; } = "";

    [StringLength(50)]
    public string Name { get; set; } = "";

    [StringLength(50)]
    public string AdditionLine { get; set; } = "";

    public string Street { get; set; } = "";
    public string Street2 { get; set; } = "";
    public string Street3 { get; set; } = "";

    [StringLength(50)]
    public string Zip { get; set; } = "";

    [StringLength(100)]
    public string City { get; set; } = "";

    [StringLength(100)]
    public string State { get; set; } = "";

    [StringLength(100)]
    public string Country { get; set; } = "";

    public string PaymentSlipAddress { get; set; } = "";
}
