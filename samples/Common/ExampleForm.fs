module ExampleForm

open Feliz
open Browser
open FormSharp.Core
open Model
open Helpers.State

let formDefinition = [
  Group [
    Title "Basic details"
    Description "Please update your personal details"
    Children [
      TextInput [
        InputProp.Getter (fun person -> person.Surname)
        InputProp.Setter (fun person value -> { person with Surname = value })
        Validate requiredNameValidator
        Label "Surname"
      ]
      #if FABLE_COMPILER
      Custom (fun props ->
        Html.input [
          prop.className "text-sm text-center italic bg-blue-100"
          prop.value props.Model.Surname
          prop.onChange (fun (v:string) -> console.log v ; props.UpdateModel { props.Model with Surname = v })
        ]
      )
      #endif
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
      Select [
        SelectProp.Getter (fun person -> person.Role)
        SelectProp.Setter (fun person newValue -> { person with Role = newValue })
        HttpItems (HttpEndpoint<DropdownItem<Role> list>.WithGet $"http://localhost:5000/roles" (createJsonDecoder ()))
        SelectProp.Label "Role"
      ]
      CheckBox [
        CheckBoxProp.CheckBoxGetter (fun person -> person.IsAuthorized)
        CheckBoxProp.CheckBoxSetter (fun person newValue -> { person with IsAuthorized = newValue })
        CheckBoxProp.Label "Is person authorized"
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
