module ExampleForm

open FormSharp.Core
open Model
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
