module ExampleForm

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

open Helpers.State

let formDefinition = [
  Group [
    Title "Basic details"
    Description "We need a few personal details from you"
    Children [
      TextInput [
        InputProp.Getter (fun person -> person.Surname)
        InputProp.Setter (fun person value -> { person with Surname = value })
        Validate requiredNameValidator
        Label "Surname"
      ]
      TextInput [
        InputProp.Getter (fun person -> person.Forename)
        InputProp.Setter (fun person value -> { person with Forename = value })
        Validate requiredNameValidator
        Label "Forename"
      ]
      DateInput [
        InputProp.Getter (fun person -> person.DateOfBirth)
        InputProp.Setter (fun person value -> { person with DateOfBirth = value })
        Validate minimumOf18YearsOld
        Label "Date of birth"
      ]
      Dropdown [
        DropdownProp.Getter (fun person -> person.Role)
        DropdownProp.Setter (fun person newValue -> { person with Role = newValue })
        HttpItems (HttpEndpoint<DropdownItem<Role> list>.WithGet $"http://localhost:5000/roles" (createJsonDecoder ()))
        AllowEmpty
        DropdownProp.Label "Role"
      ]
      Table [
        TableProp.Label "Comments"
        Collection.getter (fun person -> person.Comments)
        Collection.setter (fun action person item -> { person with Comments = person.Comments |> updateList action item })
        AddButton (fun _ _ -> Comment.Empty )
        NoItemsMessage "There are no comments."
        Columns [
          (
            [TableColumnProp.Title "Comment"], TextInput [
              InputProp.Getter (fun comment -> comment.Note)
              InputProp.Setter (fun comment value -> { comment with Note = value })
            ]            
          )
          (
            [TableColumnProp.Title "Recorded at"],
            DateInput [
              InputProp.Getter (fun comment -> comment.RecordedAt)
              InputProp.Setter (fun comment value -> { comment with RecordedAt = value })
            ]
          )
        ]
      ]
    ]
  ]
]
