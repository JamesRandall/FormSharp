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
open Feliz.Router

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
  let options =    
    match mode with
    | RemotingMode.FableRemoting ->
      [
        OnChange (fun state -> console.log state)
        OnComplete (fun _ -> Router.navigate "complete")
        Load (fun () -> async { return! personStore.get resourceId })
        Save (fun state -> async {
          do! personStore.update state
          return Ok ()
        })
      ]
    | RemotingMode.HttpEndpoint ->
      [
        OnChange (fun state -> console.log state)
        OnComplete (fun _ -> Router.navigate "complete")
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
        form
    ]
  ]
  
let Complete =
  Layout.page [
    Layout.panel [      
      Html.div [ prop.className "text-sm" ; prop.text "Thanks for updating your details!" ]
    ]
  ]
  
let ModeSelection () =
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
            Html.a [
              prop.text "Fable Remoting"
              prop.className "text-center px-4 py-2 text-white text-sm font-bold bg-blue-600 rounded-md hover:bg-blue-800"
              prop.href "#/fableRemoting"              
              prop.id "btn-fable-remoting"
            ]
            Html.a [
              prop.text "HTTP Endpoint"
              prop.className "text-center px-4 py-2 text-white text-sm font-bold bg-green-600 rounded-md hover:bg-green-800"
              //prop.onClick (fun _ -> setMode (Some RemotingMode.HttpEndpoint))
              prop.id "btn-http-endpoint"
              prop.href "#/httpEndpoint"
            ]
            Html.div [ prop.className "col-span-2" ]
          ]
        ]
      ]  
    ]
  
[<ReactComponent>]
let Router () =
  let currentUrl, updateUrl = React.useState(Router.currentUrl())
  React.router [
    router.onUrlChanged updateUrl
    router.children [
      match currentUrl with
      | [] -> ModeSelection()
      | [ "fableRemoting" ] -> FormContainer RemotingMode.FableRemoting
      | [ "httpEndpoint" ] -> FormContainer RemotingMode.HttpEndpoint
      | [ "complete" ] -> Complete
      | otherwise -> Html.h1 "Not found"
    ]
  ]
  

ReactDOM.render(
  Router,
  document.getElementById "feliz-app"
)