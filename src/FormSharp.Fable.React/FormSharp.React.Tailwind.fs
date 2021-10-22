module FormSharp.Tailwind

open Core
open FableCore
open Feliz
open System
open Fable.Core.JS

module Component =

  [<ReactComponent>]
  let EditorGroup nameOption (componentPrefix:string) (depth:int) (cIndex:int) labelOption editor =
    Html.div [
      prop.key (getComponentKey componentPrefix depth cIndex)
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
                  match nameOption with | Some name -> prop.htmlFor name | _ -> ()
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
  let Dropdown (componentPrefix:string) depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty state (props:SelectProp<'formType, 'dropdownValueType> list) =
    let items,setItems =
      React.useState(
        props |> List.tryPick(function | SelectProp.Items items -> Some items | _ -> None) |> Option.defaultValue []
      )
    let isTouched, setIsTouched = React.useState false
    let isLoading, setIsLoading = React.useState (props |> List.tryFind(function | SelectProp.HttpItems _ -> true | _ -> false) |> Option.isSome)
    
    React.useEffect((fun _ ->
      let endpointOption = props |> List.tryPick(function | SelectProp.HttpItems endpoint -> Some endpoint | _ -> None)
      match endpointOption with
      | Some endpoint -> promise {
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
      updateState ({ state with Model = func state.Model (items.[itemIndex] |> fst) ; IsDirty = true}:RendererState<'formType>)
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
      |> List.tryPick (function | SelectProp.Label label -> Some label | _ -> None)    
    let hasLabel = labelOption |> Option.isSome

    let borderColor, errorContentColor, shadowStyling = getCommonStyling isTouched showValidationWhenNotDirty isInGrid validationResult
    
    let select =
      Html.div [
        prop.className (if hasLabel then "mt-1" else "")
        prop.children [
          Html.select [
            prop.className $"{borderColor} {shadowStyling} focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm rounded-md disabled:bg-gray-50"
            prop.value value
            prop.name (getDropdownComponentName props depth cIndex)
            propOnChange
            prop.disabled (isFormDisabled || isLoading)
            prop.children (
              items
              |> List.mapi(fun i (_,description) -> Html.option [ prop.key i ; prop.value i ; prop.text description ])
            )
          ]
          match errorContentColor with | Some (msg,color) -> Html.span [prop.className $"{color} text-sm" ; prop.text msg] | _ -> React.fragment []
        ]
      ]
      
    if hasLabel then
      EditorGroup
        ((getDropdownComponentName props depth cIndex) |> Some)
        componentPrefix
        depth
        cIndex        
        labelOption
        select
    else
      Html.div [
        prop.key $"{getComponentKey componentPrefix depth cIndex}"
        prop.children select
      ]
    
    

  [<ReactComponent>]  
  let Input (componentPrefix:string) depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty type' state props =
      let isTouched,setIsTouched = React.useState false    
    
      let convertToPropValue value =
        match value with
        | Helpers.Value.Text v -> prop.value v
        | Helpers.Value.Int v -> prop.value v
        | Helpers.Value.Date v -> prop.value v
        | Helpers.Value.Float v -> prop.value v
        | Helpers.Value.Boolean v -> prop.value v
      
      let propOnChange =
        props
        |> Helpers.getInputPropertySetter
        |> (function        
          | Some (PropertySetter.IntSetter func) ->
            (prop.onChange (fun (nv:int) -> updateState ({ state with Model = func state.Model nv ; IsDirty = true}:RendererState<'formType>) ; setIsTouched true))
          | Some (PropertySetter.TextSetter func) ->
            (prop.onChange (fun (nv:string) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | Some (PropertySetter.DateTimeSetter func) ->
            (prop.onChange (fun (nv:DateTime) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | Some (PropertySetter.FloatSetter func) ->
            (prop.onChange (fun (nv:float) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
          | Some (PropertySetter.BooleanSetter func) ->
            (prop.onChange (fun (nv:bool) -> updateState { state with Model = func state.Model nv ; IsDirty = true} ; setIsTouched true))
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
              prop.name (getInputComponentName props depth cIndex)
            ]
            (match errorContentColor with | Some (msg,color) -> Html.span [prop.className $"{color} text-sm" ; prop.text msg] | _ -> React.fragment [])
          ]
        ]
        
      if hasLabel then      
        EditorGroup
          ((getInputComponentName props depth cIndex) |> Some)
          componentPrefix
          depth
          cIndex
          labelOption
          editor
      else
        Html.div [
          prop.key $"{getComponentKey componentPrefix depth cIndex}"
          prop.children editor
        ]        
        
  [<ReactComponent>]  
  let CheckBox (componentPrefix:string) depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty state props =
      let isTouched,setIsTouched = React.useState false    
    
      let propOnChange =
        props
        |> Helpers.getCheckBoxPropertySetter
        |> (function        
          | Some func ->
            (prop.onChange (fun (nv:bool) -> updateState ({ state with Model = func state.Model nv ; IsDirty = true}:RendererState<'formType>) ; setIsTouched true))          
          | _ ->
            (prop.onChange (fun (_:Browser.Types.Event) -> setIsTouched true))
        )
        
      let value =
        Helpers.getCheckBoxPropertyGetter props
        |> Option.map(fun getter -> getter state.Model)
        |> Option.defaultValue false
        
      let validationResult =
        Helpers.getCheckBoxValidator props
        |> Option.map(fun validator -> validator value)
        |> Option.defaultValue ValidationResult.Ok
      
      let _, errorContentColor, shadowStyling = getCommonStyling isTouched showValidationWhenNotDirty isInGrid validationResult        
      
      let labelOption =
        props
        |> List.tryPick (function | CheckBoxProp.Label label -> Some label | _ -> None)    
      
      Html.div [
        prop.key $"{getComponentKey componentPrefix depth cIndex}"
        prop.className "max-w-lg space-y-4"
        prop.children [
          Html.div [
            prop.className "relative flex items-start"
            prop.children [
              Html.div [
                prop.className $"flex items-center h-5"
                prop.children [
                  Html.input [
                    prop.className $"focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 rounded {shadowStyling}"
                    prop.name (getCheckBoxComponentName props depth cIndex)
                    prop.isChecked value
                    prop.type' "checkbox"
                    prop.disabled isFormDisabled
                    propOnChange
                  ]
                ]
              ]
              match labelOption with
              | Some label ->
                Html.div [
                  prop.className "ml-3 text-sm"
                  prop.children [
                    Html.label [
                      prop.htmlFor (getCheckBoxComponentName props depth cIndex)
                      prop.className "font-medium text-gray-700"
                      prop.text label
                    ]
                  ]
                ]
              | None -> ()
            ]
          ]
          (match errorContentColor with | Some (msg,color) -> Html.span [prop.className $"{color} text-sm" ; prop.text msg] | _ -> React.fragment [])
        ]
      ]                

  // Note that in order to have tables in side other tables we would need to pass down the updater and wrap it in the parent updater
  let inline Table (componentPrefix:string) (depth:int) (cIndex:int) updateState isFormDisabled _ showValidationWhenNotDirty renderComponent rendererState props =
    let columns =
      props
      |> List.tryPick(function | TableProp.Columns columns -> Some columns | _ -> None)
      |> Option.defaultValue []
    let headers =
      columns
      |> List.mapi(fun i (columnProps, _) ->
        let title =
          columnProps
          |> List.tryPick(function | TableColumnProp.Title t -> Some t | _ -> None)
          |> Option.defaultValue ""
        Html.th [
          prop.className "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          prop.text title
          prop.key i
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
              |> List.mapi(fun i (_, contentComponent) ->
                Html.td [
                  prop.key i
                  prop.className "px-6 py-2 whitespace-nowrap text-gray-900"
                  prop.children [
                    renderComponent componentPrefix (depth+1) cIndex isFormDisabled true showValidationWhenNotDirty childRenderState updateRowState contentComponent
                  ]
                ]
              )
            Html.tr [
              prop.key (getComponentKey componentPrefix (depth+1) cIndex)
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
        None
        componentPrefix
        depth
        cIndex
        labelOption
        wrappedTable
    else
      Html.div [
        prop.key $"{getComponentKey componentPrefix depth cIndex}"
        prop.children wrappedTable
      ]
      
  [<ReactComponent>]
  let CustomFormComponent<'formType>
      (props:CustomComponentProps<'formType>)
      (elementFunc:CustomComponentProps<'formType> -> Fable.React.ReactElement) =
    elementFunc props    

  let rec render componentPrefix depth cIndex isFormDisabled isInGrid showValidationWhenNotDirty state updateState componentDefinition =
    let renderGroup _ props =
      let title =
        props
        |> List.tryPick(function
          | GroupProp.Title title ->
            Html.h3 [
              prop.key $"{getComponentKey componentPrefix depth cIndex}_grouptitle"
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
              prop.key $"{getComponentKey componentPrefix depth cIndex}_groupdescription"
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
              prop.key $"{getComponentKey componentPrefix depth cIndex}_groupchildren"
              prop.className "mt-6 sm:mt-5 space-y-6 sm:space-y-5"
              prop.children (children |> List.mapi (fun i c -> render componentPrefix (depth+1) i isFormDisabled false showValidationWhenNotDirty state updateState c)) 
            ] |> Some
          | _ -> None
        )      
        |> Option.defaultValue (React.fragment [])
      
      Html.div [
        prop.className "space-y-8 divide-y divide-gray-200"
        prop.key (getComponentKey componentPrefix depth cIndex)
        prop.children (Html.div [ title ; description ; children])        
      ]            
    
    // TODO: come back to this and resolve more elegantly - see note on shims as to what this is all about
    Shims.TextInputShim.renderer <- Input componentPrefix depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty "text" 
    Shims.DateInputShim.renderer <- Input componentPrefix depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty "date" 
    Shims.GroupShim.renderer <- renderGroup
    Shims.TableShim.renderer <- Table componentPrefix depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty render
    Shims.DropdownShim.renderer <- Dropdown componentPrefix depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty
    Shims.BooleanInputShim.renderer <- CheckBox componentPrefix depth cIndex updateState isFormDisabled isInGrid showValidationWhenNotDirty
    Shims.CustomShim.renderer <- CustomFormComponent
    
    componentDefinition.render
      state
      { UpdateModel = (fun newModel -> updateState { state with Model = newModel ; IsDirty = true })
        ComponentDepth = 0
        ComponentIndex =0
      }
  
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
      Component.render formId 0 i isFormDisabled false showValidationWhenNotDirty state updateState c
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
            |> List.mapi(fun i button ->
              match button with
              | Button.Save ->
                Html.button [
                  prop.className "bg-blue-600 text-white rounded-md py-1 px-6 hover:bg-blue-400 disabled:bg-blue-400" 
                  prop.text "Save"
                  prop.type' "submit"
                  prop.disabled isButtonDisabled
                  prop.onClick (fun _ -> saveFunc state |> ignore)
                  prop.key i
                ]
              | Button.Cancel ->
                Html.button [
                  prop.className "bg-orange-600 text-white rounded-md py-1 px-6 hover:bg-orange-400 disabled:bg-orange-400" 
                  prop.text "Cancel"
                  prop.disabled isButtonDisabled
                  prop.key i
                ]
            ))
          )
        ]
    ]
  ]