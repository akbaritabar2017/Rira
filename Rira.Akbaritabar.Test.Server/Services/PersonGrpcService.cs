using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Rira.Akbaritabar.Test.Server.Data;
using Rira.Akbaritabar.Test.Server.Models;

namespace Rira.Akbaritabar.Test.Server.Services;

public partial class PersonGrpcService(AppDbContext db, ILogger<PersonGrpcService> logger) : PersonService.PersonServiceBase
{
    #region Public Methods

    public override Task<PersonReply> CreatePerson(
        CreatePersonRequest request,
        ServerCallContext   context
    )
    {
        return Call(async () =>
            {
                string? nationalCode = request.NationalCode?.Trim();
                string? name         = request.Name?.Trim();
                string? family       = request.Family?.Trim();

                CheckPersonInfo(name, family, nationalCode);

                bool nationalCodeDuplicated = await db.Persons.AnyAsync(x => x.NationalCode == nationalCode);
                if (nationalCodeDuplicated)
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "NationalCode is already exists"));

                var person = new Person
                {
                    Name         = name!,
                    Family       = family!,
                    NationalCode = nationalCode!,
                    BirthDate    = request.BirthDate.ToDateTime()
                };

                db.Persons.Add(person);

                await db.SaveChangesAsync();

                return Map(person);
            }
        );
    }

    public override Task<PersonReply> GetPerson(
        PersonByIdRequest request,
        ServerCallContext context
    )
    {
        return Call(async () =>
            {
                Person? person = await db.Persons.FindAsync(request.Id);

                return person == null ? throw new RpcException(new Status(StatusCode.NotFound, "Person not found")) : Map(person);
            }
        );
    }

    public override Task<PersonsReply> GetAllPersons(
        Empty             request,
        ServerCallContext context
    )
    {
        return Call(async () =>
            {
                Person[] persons = await db.Persons.ToArrayAsync();
                var      result  = new PersonsReply();
                result.Persons.AddRange(persons.Select(Map));

                return result;
            }
        );
    }

    public override Task<PersonReply> UpdatePerson(
        UpdatePersonRequest request,
        ServerCallContext   context
    )
    {
        return Call(async () =>
            {
                Person? person = await db.Persons.FindAsync(request.Id);

                if (person == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Person not found"));

                string? nationalCode = request.NationalCode?.Trim();
                string? name         = request.Name?.Trim();
                string? family       = request.Family?.Trim();

                CheckPersonInfo(name, family, nationalCode);

                bool nationalCodeDuplicated = await db.Persons.AnyAsync(x => x.NationalCode == nationalCode && x.Id != request.Id);
                if (nationalCodeDuplicated)
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "NationalCode is for an other person"));

                person.Name         = name!;
                person.Family       = family!;
                person.NationalCode = nationalCode!;
                person.BirthDate    = request.BirthDate.ToDateTime();

                await db.SaveChangesAsync();

                return Map(person);
            }
        );
    }

    public override Task<DeleteReply> DeletePerson(
        PersonByIdRequest request,
        ServerCallContext context
    )
    {
        return Call(async () =>
            {
                Person? person = await db.Persons.FindAsync(request.Id);

                if (person == null)
                    return new DeleteReply { Success = false };

                db.Persons.Remove(person);

                await db.SaveChangesAsync();

                return new DeleteReply { Success = true };
            }
        );
    }

    #endregion

    #region Private Methods

    private static PersonReply Map(Person person)
    {
        return new PersonReply
        {
            Id           = person.Id,
            Name         = person.Name,
            Family       = person.Family,
            NationalCode = person.NationalCode,
            BirthDate    = Timestamp.FromDateTime(person.BirthDate)
        };
    }

    private static void CheckPersonInfo(string? name, string? family, string? nationalCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required"));

        if (string.IsNullOrWhiteSpace(family))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Family is required"));

        if (string.IsNullOrWhiteSpace(nationalCode))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "NationalCode is required"));

        if (!IsValidNationalCode(nationalCode))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "NationalCode is invalid"));
    }

    public static bool IsValidNationalCode(string nationalCode)
    {
        if (string.IsNullOrWhiteSpace(nationalCode))
            return false;

        nationalCode = nationalCode.Trim();

        if (!NationalCodeRegex().IsMatch(nationalCode))
            return false;

        string[] invalidCodes =
        {
            "0000000000", "1111111111", "2222222222", "3333333333", "4444444444",
            "5555555555", "6666666666", "7777777777", "8888888888", "9999999999"
        };

        if (invalidCodes.Contains(nationalCode))
            return false;

        int check = int.Parse(nationalCode[9].ToString());
        int sum   = 0;

        for (int i = 0; i < 9; i++)
            sum += (nationalCode[i] - '0') * (10 - i);

        int remainder = sum % 11;

        return (remainder < 2 && check == remainder) || (remainder >= 2 && check == (11 - remainder));
    }

    private Task<T> Call<T>(Func<Task<T>> func)
    {
        try
        {
            return func();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled error");
            throw new RpcException(new Status(StatusCode.Internal, "Unexpected error occurred"));
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{10}$")]
    private static partial System.Text.RegularExpressions.Regex NationalCodeRegex();

    #endregion
}
