using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Rira.Akbaritabar.Test.Server.Models;

[Index(nameof(NationalCode), IsUnique = true)]
public class Person
{
    public int Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = "";

    [StringLength(128)]
    public string Family { get; set; } = "";

    [StringLength(10)]
    public string NationalCode { get; set; } = "";

    public DateTime BirthDate { get; set; }
}
