﻿module FormSharp.Fable.Core

open Fable.Core.JS
open Fetch.Types
open FormSharp.Core

type RendererState<'formType> =
  { Model: 'formType
    ComponentsLoading: int
    IsSaving: bool
    IsDisabled: bool
    IsDirty: bool
    ErrorMessage: string option
  }
  member x.IsLoading = x.ComponentsLoading > 0
  member x.ComponentLoading () = { x with ComponentsLoading = x.ComponentsLoading + 1 }
  member x.ComponentFinishedLoading () =
    { x with ComponentsLoading = if x.ComponentsLoading > 0 then x.ComponentsLoading - 1 else 0 }
  
let convertToHttpMethod verb =
  match verb with
  | HttpVerb.Get -> HttpMethod.GET
  | HttpVerb.Put -> HttpMethod.PUT
  | HttpVerb.Patch -> HttpMethod.PATCH
  | HttpVerb.Post -> HttpMethod.POST

let inline executeHttpWithResult<'responseType> (httpEndpoint:HttpEndpoint<'responseType>) bodyOption = promise {
  match httpEndpoint.ResponseDecoder with
  | Some decoder ->
    let! result = Fetch.tryFetch httpEndpoint.Url [
        RequestProperties.Method (httpEndpoint.Verb |> convertToHttpMethod)
        match bodyOption with | Some body -> RequestProperties.Body  <| unbox(Thoth.Json.Encode.Auto.toString (2,body)) | _ ->()      
      ]
    match result with
    | Ok response ->
      let! responseText = response.text ()
      console.log responseText
      let responseModel = decoder responseText    
      return Ok responseModel
    | Error ex -> return Error $"Error with error {ex}"
  | None -> return Error $"Missing decoder"
}

let inline executeHttp httpEndpoint bodyOption = promise {
  let! result = Fetch.tryFetch httpEndpoint.Url [
      RequestProperties.Method (httpEndpoint.Verb |> convertToHttpMethod)
      match bodyOption with | Some body -> RequestProperties.Body  <| unbox(JSON.stringify body) | _ ->()      
    ]
  match result with
  | Ok _ ->
    return Ok ()
  | Error ex -> return Error $"Error with error {ex}"
}

let inline createLoadFunction setState httpEndpointOption =
  match httpEndpointOption with
  | None ->
    (fun (rendererState:RendererState<_>) -> promise { setState ( rendererState.ComponentFinishedLoading () ) })
  | Some endpoint -> (fun (rendererState:RendererState<_>) -> promise {
      setState ( rendererState.ComponentLoading () )
      let! result = executeHttpWithResult endpoint None
      match result with
      | Ok model ->
        setState { rendererState.ComponentFinishedLoading () with Model = model }
      | Error e ->
        console.error e
        setState { rendererState.ComponentFinishedLoading () with ErrorMessage = Some "Unable to load resource" }
    })
  
let createSaveFunction options setState httpEndpointOption =
  let saveError = "Unable to save, please try again."
  match httpEndpointOption with
  | None -> (fun rendererState -> promise { options.OnComplete rendererState.Model })
  | Some endpoint -> (fun rendererState -> promise {
      setState({rendererState with IsSaving = true ; ErrorMessage = None})
      let! result = Some rendererState.Model |> executeHttp endpoint                     
      match result with
      | Ok _ ->
        setState({rendererState with IsSaving = false })
        options.OnComplete rendererState.Model
      | Error _ -> setState({rendererState with IsSaving = false ; ErrorMessage = Some saveError })
    })

