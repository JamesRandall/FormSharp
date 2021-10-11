module FormSharp.Fable.React.Tailwind

open FormSharp.Core
open FormSharp.Fable.Core
open Feliz
open System
open Fable.Core.JS

module Component =

  [<ReactComponent>]
  let EditorGroup (componentKey:string) labelOption editor =
    Html.div [
      prop.key componentKey
      prop.className "mt-6 gap-y-6 gap-x-4"
      prop.children [
        Html.div [
          prop.className ""
          prop.children [
            (
              match labelOption with
              | Some (label:string) ->
                Html.label [
                  prop.className "block text-sm font-medium text-gray-700"
                  prop.text label
                ]
              | None -> React.fragment []            
            )
            editor
          ]
        ]
      ]
    ]
    
  let getCommonStyling isTouched showValidationWhenNotDirty isInGrid validationResult =
    let shadowStyling = if isInGrid then "" else "shadow-sm"
    
    if isTouched || showValidationWhenNotDirty then
      match validationResult with
      | ValidationResult.Ok -> "border-gray-300", None, shadowStyling
      | ValidationResult.Warning msg -> "border-yellow-300", (Some (msg,"text-yellow-600")), shadowStyling
      | ValidationResult.Error msg -> "border-red-600", (Some (msg, "text-red-600")), shadowStyling
    else
      (if isInGrid then "border-gray-100" else "border-gray-300"), None, shadowStyling
    
  [<ReactComponent>]
  let Dropdown (componentKey:string) updateState state isFormDisabled isInGrid showValidationWhenNotDirty _ (props:DropdownProp<'formType, 'dropdownValueType> list) =
    let items,setItems =
      React.useState(
        props |> List.tryPick(function | DropdownProp.Items items -> Some items | _ -> None) |> Option.defaultValue []
      )
    let isTouched, setIsTouched = React.useState false
    let isLoading, setIsLoading = React.useState (props |> List.tryFind(function | DropdownProp.HttpItems _ -> true | _ -> false) |> Option.isSome)
    
    React.useEffect((fun _ ->
      let endpointOption = props |> List.tryPick(function | DropdownProp.HttpItems endpoint -> Some endpoint | _ -> None)
      match endpointOption with
      | Some (Some endpoint) -> promise {
          setIsLoading true
          let! result = executeHttpWithResult endpoint None
          match result with
          | Ok retrievedItems ->
            retrievedItems |> List.map(fun item -> item.Value,item.Label) |> setItems
          | Error e -> console.error e
          setIsLoading false
        }
      | _ -> promise { return () }
      |> ignore
    ), [||])
    
    let updateWithItemAtIndex func (itemIndexAsString:string) =
      let itemIndex = itemIndexAsString |> int
      updateState { state with Model = func state.Model (items.[itemIndex] |> fst) ; IsDirty = true}
      setIsTouched true
    
    let propOnChange =
      props
      |> Helpers.getDropdownPropertySetter
      |> (function
        | Some func -> updateWithItemAtIndex func |> prop.onChange
        | _ ->
          fun (_:Browser.Types.Event) -> setIsTouched true
          |> prop.onChange
      )
      
    let value =
      props
      |> Helpers.getDropdownPropertyGetter
      |> (function
        | Some func -> items |> List.tryFindIndex (fun (v,_) -> v = func state.Model) |> Option.defaultValue 0
        | _ -> 0
      )
    let validationResult = ValidationResult.Ok
    
    let labelOption =
      props
      |> List.tryPick (function | DropdownProp.Label label -> Some label | _ -> None)    
    let hasLabel = labelOption |> Option.isSome

    let borderColor, errorContentColor, shadowStyling = getCommonStyling isTouched showValidationWhenNotDirty isInGrid validationResult
    
    let select =
      Html.div [
        prop.className (if hasLabel then "mt-1" else "")
        prop.children [
          Html.select [
            prop.className $"{borderColor} {shadowStyling} focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm rounded-md disabled:bg-gray-50"
            prop.value value
            propOnChange
            prop.disabled (isFormDisabled || isLoading)
            prop.children (
              items
              |> List.mapi(fun i (_,description) -> Html.option [ prop.value i ; prop.text description ])
            )
          ]
          match errorContentColor with | Some (msg,color) -> Html.span [prop.className $"{color} text-sm" ; prop.text msg] | _ -> React.fragment []
        ]
      ]
      
    if hasLabel then
      EditorGroup
        componentKey
        labelOption
        select
    else
      select
    
    

  [<ReactComponent>]  
  let Input (componentKey:string) updateState state isFormDisabled isInGrid showValidationWhenNotDirty type' _ props =
      let isTouched,setIsTouched = React.useState false    
    
      let convertToPropValue value =
        match value with
        | Helpers.Value.Text v -> prop.value v
        | Helpers.Value.Int v -> prop.value v
        | Helpers.Value.Date v -> prop.value v
      
      let propOnChange =
        props
        |> Helpers.getInputPropertySetter
        |> (function        
          | Some (PropertySetter.IntSetter func) ->
            (prop.onChange (fun (nv:int) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | Some (PropertySetter.TextSetter func) ->
            (prop.onChange (fun (nv:string) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | Some (PropertySetter.DateTimeSetter func) ->
            (prop.onChange (fun (nv:DateTime) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | _ ->
            (prop.onChange (fun (_:Browser.Types.Event) -> setIsTouched true))
        )
        
      let wrappedValue,validationResult = state.Model |> Helpers.getInputValueAndValidationState props
      let propValue = wrappedValue |> convertToPropValue
      
      let borderColor, errorContentColor, shadowStyling = getCommonStyling isTouched showValidationWhenNotDirty isInGrid validationResult        
      
      let labelOption =
        props
        |> List.tryPick (function | InputProp.Label label -> Some label | _ -> None)    
      let hasLabel = labelOption |> Option.isSome
      
      let editor =
        Html.div [
          prop.className (if hasLabel then "mt-1" else "")
          prop.children [
            Html.input [
              prop.type' type'
              prop.className $"{borderColor} {shadowStyling} focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm rounded-md disabled:bg-gray-50"
              propValue
              propOnChange
              prop.disabled isFormDisabled
            ]
            (match errorContentColor with | Some (msg,color) -> Html.span [prop.className $"{color} text-sm" ; prop.text msg] | _ -> React.fragment [])
          ]
        ]
        
      if hasLabel then      
        EditorGroup
          componentKey
          labelOption
          editor
      else
        editor

  // Note that in order to have tables in side other tables we would need to pass down the updater and wrap it in the parent updater
  let inline Table (componentKey:string) updateState rendererState isFormDisabled _ showValidationWhenNotDirty renderComponent _ props =
    let columns =
      props
      |> List.tryPick(function | TableProp.Columns columns -> Some columns | _ -> None)
      |> Option.defaultValue []
    let headers =
      columns
      |> List.map(fun (columnProps, _) ->
        let title =
          columnProps
          |> List.tryPick(function | TableColumnProp.Title t -> Some t | _ -> None)
          |> Option.defaultValue ""
        Html.th [
          prop.className "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          prop.text title
        ]
      )
    let collection =
      props
      |> List.tryPick(function | TableProp.CollectionGetter getter -> Some (getter rendererState.Model) | _ -> None)    
      |> Option.defaultValue []
    let addItemOption =
      props
      |> List.tryPick(function | TableProp.AddButton itemAdder -> Some itemAdder | _ -> None)    
    let collectionSetter:(CollectionAction -> 'formType -> 'collectionItemType -> 'formType) =
      props
      |> List.tryPick(function | TableProp.CollectionSetter setter -> Some setter | _ -> None)
      |> Option.defaultValue (fun (_:CollectionAction) (_:'formType) (_:'collectionItemType) -> rendererState.Model)
        
    let table =
      match collection |> List.isEmpty, props |> List.tryPick(function | TableProp.NoItemsMessage noItemsMessage -> Some noItemsMessage | _ -> None) with
      | true, Some noItemsMessage ->
        Html.div [
          prop.className "text-sm italic"
          prop.text noItemsMessage
        ]
      | _ ->
        let content =
          collection
          |> List.mapi(fun rowIndex row ->
            // do NOT use "with" here, it may look ok but type inference will then decide that 'formType is 'collectionItemType
            let childRenderState =
              { Model = row
                ComponentsLoading = rendererState.ComponentsLoading
                IsSaving = rendererState.IsSaving
                IsDisabled = rendererState.IsDisabled
                IsDirty = rendererState.IsDirty
                ErrorMessage = rendererState.ErrorMessage          
              }
            let updateRowState rowState =
              let newState = collectionSetter (CollectionAction.Update rowIndex) rendererState.Model rowState.Model
              updateState { rendererState with Model = newState }
            let cells =
              columns
              |> List.mapi(fun cellIndex (_, contentComponent) ->
                Html.td [
                  prop.className "px-6 py-2 whitespace-nowrap text-gray-900"
                  prop.children [
                    renderComponent $"{componentKey}_{rowIndex}_{cellIndex}" isFormDisabled true showValidationWhenNotDirty childRenderState updateRowState contentComponent
                  ]
                ]
              )
            Html.tr [
              prop.key $"{componentKey}_{rowIndex}"
              prop.className (if rowIndex % 2 = 0 then "bg-white" else "bg-gray-50")
              prop.children cells
            ]        
          )
        Html.div [
          prop.className "flex flex-col"
          prop.children [
            Html.div [
              prop.className "-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8"
              prop.children [
                Html.div [
                  prop.className "py-2 align-middle inline-block min-w-full sm:px-6 lg:px-8"
                  prop.children [
                    Html.div [
                      prop.className "shadow overflow-hidden border-b border-gray-200 sm:rounded-lg"
                      prop.children [
                        Html.table [
                          prop.className "min-w-full divide-y divide-gray-200"
                          prop.children [
                            Html.thead [
                              prop.className "bg-gray-50"
                              prop.children [
                                Html.tr headers
                              ]
                            ]
                            Html.tbody content          
                          ]
                        ]
                      ]
                    ]
                  ]
                ]
              ]
            ]
          ]
        ]
      
    let wrappedTable =  
      match addItemOption with
      | Some itemAdder ->
        let onAddHandler _ =
          let newItem = itemAdder rendererState.Model collection
          let newModel = (collectionSetter CollectionAction.Add rendererState.Model newItem)
          updateState { rendererState with Model = newModel  }          
        Html.div [
          prop.className "flex flex-row items-start"
          prop.children [
            Html.div [ prop.className "flex-grow" ; prop.children [ table ] ]
            Html.button [
              prop.className "ml-2 bg-white text-black border border-gray-300 rounded-md py-2 px-2 hover:bg-gray-200 disabled:bg-gray-50" 
              prop.disabled isFormDisabled
              prop.children [
                Svg.svg [
                  svg.viewBox (0,0,384,512)
                  svg.className "w-3 h-3"
                  svg.children [
                    Svg.path [
                      svg.d "M368 224H224V80c0-8.84-7.16-16-16-16h-32c-8.84 0-16 7.16-16 16v144H16c-8.84 0-16 7.16-16 16v32c0 8.84 7.16 16 16 16h144v144c0 8.84 7.16 16 16 16h32c8.84 0 16-7.16 16-16V288h144c8.84 0 16-7.16 16-16v-32c0-8.84-7.16-16-16-16z" 
                    ]
                  ]
                ]                
              ]
              prop.onClick onAddHandler
            ]
          ]
        ]
      | None -> table
      
    let labelOption = props |> List.tryPick(function | TableProp.Label label -> Some label | _ -> None)    
    if labelOption |> Option.isSome then      
      EditorGroup
        componentKey
        labelOption
        wrappedTable
    else
      wrappedTable

  let rec render (componentKey:string) isFormDisabled isInGrid showValidationWhenNotDirty state updateState componentDefinition =
    let renderGroup _ props =
      let title =
        props
        |> List.tryPick(function
          | GroupProp.Title title ->
            Html.h3 [
              prop.key $"{componentKey}_grouptitle"
              prop.className "text-lg leading-6 font-medium text-gray-900"
              prop.text title
            ] |> Some
          | _ -> None
        )
        |> Option.defaultValue (React.fragment [])      
      let description =
        props
        |> List.tryPick(function
          | GroupProp.Description description ->
            Html.p [
              prop.key $"{componentKey}_groupdescription"
              prop.className "mt-1 text-sm text-gray-500"
              prop.text description
            ] |> Some
          | _ -> None
        )            
        |> Option.defaultValue (React.fragment [])
      let children =
        props
        |> List.tryPick(function
          | GroupProp.Children children ->
            Html.div [
              prop.key $"{componentKey}_groupchildren"
              prop.className "mt-6 sm:mt-5 space-y-6 sm:space-y-5"
              prop.children (children |> List.mapi (fun i c -> render $"{componentKey}_{i}" isFormDisabled false showValidationWhenNotDirty state updateState c)) 
            ] |> Some
          | _ -> None
        )      
        |> Option.defaultValue (React.fragment [])
      
      Html.div [
        prop.className "space-y-8 divide-y divide-gray-200"
        prop.key componentKey
        prop.children (Html.div [ title ; description ; children])        
      ]
    
    // TODO: come back to this and resolve more elegantly - see note on shims as to what this is all about
    Shims.TextInputShim.renderer <- Input componentKey updateState state isFormDisabled isInGrid showValidationWhenNotDirty "text" 
    Shims.DateInputShim.renderer <- Input componentKey updateState state isFormDisabled isInGrid showValidationWhenNotDirty "date" 
    Shims.GroupShim.renderer <- renderGroup
    Shims.TableShim.renderer <- Table componentKey updateState state isFormDisabled isInGrid showValidationWhenNotDirty render
    Shims.DropdownShim.renderer <- Dropdown componentKey updateState state isFormDisabled isInGrid showValidationWhenNotDirty
    
    componentDefinition.render state.Model
  
[<ReactComponent>]
let Form formId
         state
         updateState
         buttons
         showValidationWhenNotDirty
         (saveFunc:RendererState<'formType> -> Promise<unit>)
         (loadFunc:RendererState<'formType> -> Promise<unit>)
         formDefinition =  
  let isValidForSave =
    state.Model
    |> Helpers.validateForm formDefinition
    |> (function | ValidationResult.Error _ -> false | _ -> true)
    
  let isButtonDisabled =
    state.IsDisabled || not(state.IsDirty) || not(isValidForSave) || state.IsSaving || state.IsLoading
  let isFormDisabled = state.IsDisabled || state.IsSaving || state.IsLoading
  
  let formContent =
    formDefinition
    |> List.mapi (fun i c ->
      Component.render $"{formId}_{i}" isFormDisabled false showValidationWhenNotDirty state updateState c
    )
    
  React.useEffect((fun _ ->
    loadFunc state |> ignore 
  ), [||])
  
  Html.div [
    prop.children [
      Html.div formContent
      if buttons |> List.isEmpty then
        React.fragment []
      else
        Html.div [
          prop.className "mt-6 flex flex-row justify-between"
          prop.children (
            (Html.div [
              match (state.IsSaving || state.IsLoading), state.ErrorMessage with
              | true,_ ->              
                Html.div [
                  prop.className "flex justify-center items-center"
                  prop.children [
                    Html.div [ prop.className "loader ease-linear rounded-full border-4 border-t-4 border-gray-200 h-8 w-8" ]
                  ]
                ]
              | false,Some errorMessage -> Html.div [ prop.text errorMessage ; prop.className "text-red-600 text-sm" ]
              | _ -> React.fragment []
            ])
            ::
            (buttons
            |> List.map(fun button ->
              match button with
              | Button.Save ->
                Html.button [
                  prop.className "bg-blue-600 text-white rounded-md py-1 px-6 hover:bg-blue-400 disabled:bg-blue-400" 
                  prop.text "Save"
                  prop.disabled isButtonDisabled
                  prop.onClick (fun _ -> saveFunc state |> ignore)
                ]
              | Button.Cancel ->
                Html.button [
                  prop.className "bg-orange-600 text-white rounded-md py-1 px-6 hover:bg-orange-400 disabled:bg-orange-400" 
                  prop.text "Cancel"
                  prop.disabled isButtonDisabled
                ]
            ))
          )
        ]
    ]
  ]