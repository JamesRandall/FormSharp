module Model

open FormSharp.Core
open System

type Comment =
  { Id: Guid
    Note: string
    RecordedAt: DateTime
  }
  static member Empty = {
    Id = Guid.Empty
    Note = String.Empty
    RecordedAt = DateTime.Now
  }
  
[<RequireQualifiedAccess>]
type Role =
  | Administrator
  | Shopper

type Person =
  { Id: Guid
    Surname: string
    Forename: string
    DateOfBirth: DateTime
    Comments: Comment list
    Role: Role
    IsAuthorized: bool
  }
  static member Empty = {
    Id = Guid.Empty
    Surname = String.Empty
    Forename = String.Empty
    DateOfBirth = DateTime.Now.AddYears(-25)
    Comments = []
      //{ Id = Guid.NewGuid() ; Note = "Some text" ; RecordedAt = DateTime.Now }
      //{ Id = Guid.NewGuid() ; Note = "Some different text" ; RecordedAt = DateTime.Now.AddDays(-1.) }
    //]
    Role = Role.Shopper
    IsAuthorized = false
  }
  
let requiredNameValidator surname =
  if String.IsNullOrWhiteSpace(surname) then
    ValidationResult.Error "Surname is required"
  elif surname.Length > 50 then
    ValidationResult.Error "Surname must be no longer than 50 characters"
  else
    ValidationResult.Ok
    
let minimumOf18YearsOld (dateOfBirth:DateTime) =
  if dateOfBirth.AddYears(18) > DateTime.Now then
    ValidationResult.Error "Must be at least 18 years old"
  else
    ValidationResult.Ok
