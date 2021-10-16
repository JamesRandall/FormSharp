module App

open Feliz
open Browser.Dom
open FormSharp.Core
open FormSharp.Fable
open FormSharp.Fable.React
open Fable.Core.JsInterop
open ExampleForm
open Model
open Fable.Remoting.Client


importAll "./styles.css"

[<RequireQualifiedAccess>]
type RemotingMode =
  | FableRemoting
  | HttpEndpoint
  
let personStore : IPersonStore =
  Remoting.createApi()
  |> Remoting.withBaseUrl "http://localhost:5000"
  |> Remoting.buildProxy<IPersonStore>

[<ReactComponent>]
let FormContainer mode =
  let resourceId = System.Guid.Parse "0EB0F488-832F-4144-8492-0CFE73200347"
  let isComplete, setIsComplete = React.useState(false)
  let options =    
    match mode with
    | RemotingMode.FableRemoting ->
      [
        OnChange (fun state -> console.log state)
        OnComplete (fun _ -> setIsComplete true)
        Load (fun () -> async { return! personStore.get resourceId })
        Save (fun state -> async {
          do! personStore.update state
          return Ok ()
        })
      ]
    | RemotingMode.HttpEndpoint ->
      [
        OnChange (fun state -> console.log state)
        OnComplete (fun _ -> setIsComplete true)
        LoadFromUrl (HttpEndpoint.WithGet $"http://localhost:5000/person/{resourceId}" (createJsonDecoder ()))
        SaveToUrl (HttpEndpoint.WithPut "http://localhost:5000/person")
      ]
  let form =
    React.useForm(
      Person.Empty,
      formDefinition,
      Tailwind.Form,
      options      
    ) 
  
  Layout.page [
    
    Html.h1 [
      prop.className "text-3xl text-center"
      prop.text (
        match mode with
        | RemotingMode.FableRemoting -> "Using Fable Remoting"
        | RemotingMode.HttpEndpoint -> "Using HTTP Endpoints"
      )
    ]
    
    Layout.panel [
      if isComplete then
        Html.div [ prop.className "text-sm" ; prop.text "Thanks for updating your details!" ]
      else
        form
    ]
  ]
  
[<ReactComponent>]
let ModeSelection () =
  let modeOption, setMode = React.useState(None)
  match modeOption with
  | None ->
    Html.div [
      prop.className "mt-12"
      prop.children [
        Html.h1 [
          prop.className "text-3xl text-center"
          prop.text "FormSharp Tailwind Demonstration"
        ]
        Html.div [
          prop.className "mt-12 grid grid-cols-6 gap-x-4"
          prop.children [
            Html.div [ prop.className "col-span-2" ]
            Html.button [
              prop.text "Fable Remoting"
              prop.className "px-4 py-2 text-white text-sm font-bold bg-blue-600 rounded-md hover:bg-blue-800"
              prop.onClick (fun _ -> setMode (Some RemotingMode.FableRemoting))
              prop.id "btn-fable-remoting"
            ]
            Html.button [
              prop.text "HTTP Endpoint"
              prop.className "px-4 py-2 text-white text-sm font-bold bg-green-600 rounded-md hover:bg-green-800"
              prop.onClick (fun _ -> setMode (Some RemotingMode.HttpEndpoint))
              prop.id "btn-http-endpoint"
            ]
            Html.div [ prop.className "col-span-2" ]
          ]
        ]
      ]  
    ]
  | Some mode -> FormContainer mode

ReactDOM.render(
  ModeSelection,
  document.getElementById "feliz-app"
)