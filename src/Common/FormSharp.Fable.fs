namespace FormSharp

module FableCore =
  open Fable.Core
  open Fable.Core.JS
  open Fetch.Types
  open FormSharp.Core

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

  let inline createLoadFunction options setState =
    let processResult (rendererState:RendererState<_>) result =
      match result with
      | Ok model ->
        setState { rendererState.ComponentFinishedLoading () with Model = model }
      | Error e ->
        console.error e
        setState { rendererState.ComponentFinishedLoading () with ErrorMessage = Some "Unable to load resource" }
    
    match options.Load with
    | Some asyncLoadFunc ->
      (fun (rendererState:RendererState<_>) -> promise {
        setState ( rendererState.ComponentLoading () )
        let! result = asyncLoadFunc () |> Async.StartAsPromise
        result |> processResult rendererState
      })    
    | None ->
      match options.LoadFromUrl with    
      | Some endpoint -> (fun (rendererState:RendererState<_>) -> promise {
          setState ( rendererState.ComponentLoading () )
          let! result = executeHttpWithResult endpoint None
          result |> processResult rendererState
        })
      | None ->
        (fun (rendererState:RendererState<_>) -> promise { setState ( rendererState.ComponentFinishedLoading () ) })
    
  let inline createSaveFunction options setState =
    let saveError = "Unable to save, please try again."
    let processResult (rendererState:RendererState<_>) result =
      match result with
      | Ok _ ->
        setState({rendererState with IsSaving = false })
        options.OnComplete rendererState.Model
      | Error _ -> setState({rendererState with IsSaving = false ; ErrorMessage = Some saveError })
      
    match options.Save with
    | Some asyncSaveFunc ->
      (fun rendererState -> promise {
        setState({rendererState with IsSaving = true ; ErrorMessage = None})
        let! result = asyncSaveFunc rendererState.Model |> Async.StartAsPromise                     
        result |> processResult rendererState
      })
    | None ->
      match options.SaveToUrl with
      | None -> (fun rendererState -> promise { options.OnComplete rendererState.Model })
      | Some endpoint -> (fun rendererState -> promise {
          setState({rendererState with IsSaving = true ; ErrorMessage = None})
          let! result = Some rendererState.Model |> executeHttp endpoint                     
          result |> processResult rendererState
        })

  let getComponentName (labelOption:string option) depth index =
    match labelOption with
    | Some label ->
      let labelWithoutSpaces = label.Replace(" ", "_")
      $"input_{depth}_{index}_{labelWithoutSpaces}"
    | None -> $"input_{depth}_{index}"

  let getInputComponentName inputProps =
    getComponentName (inputProps |> List.tryPick(function | InputProp.Label l -> Some l | _ -> None))
    
  let getCheckBoxComponentName inputProps =
    getComponentName (inputProps |> List.tryPick(function | CheckBoxProp.Label l -> Some l | _ -> None))
    
  let getDropdownComponentName dropdownProps =
    getComponentName (dropdownProps |> List.tryPick(function | SelectProp.Label l -> Some l | _ -> None))

  let getComponentKey prefix depth index = $"{prefix}_{depth}_{index}"  