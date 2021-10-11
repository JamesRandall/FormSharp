module App

open Feliz
open Browser.Dom
open FormSharp.Core
open FormSharp.Fable
open FormSharp.Fable.React
open Fable.Core.JsInterop
open ExampleForm

importAll "./styles.css"

[<ReactComponent>]
let Page () =
  let resourceId = System.Guid.Parse "0EB0F488-832F-4144-8492-0CFE73200347"
  let isComplete, setIsComplete = React.useState(false)
  let form =
    React.useForm(
      Person.Empty,
      formDefinition,
      Tailwind.Form,
      { FormOptions.Default with
          SaveToUrl = HttpEndpoint.WithPut "http://localhost:5000/person"
          OnChange = (fun state -> console.log state)
          OnComplete = (fun _ -> setIsComplete true)
          LoadFromUrl = HttpEndpoint<Person>.WithGet $"http://localhost:5000/person/{resourceId}" (createJsonDecoder ())
      }
    ) 
  
  Layout.page [
    Layout.panel [
      if isComplete then
        Html.div [ prop.className "text-sm" ; prop.text "Thanks for updating your details!" ]
      else
        form
    ]
  ]  

ReactDOM.render(
  Page,
  document.getElementById "feliz-app"
)