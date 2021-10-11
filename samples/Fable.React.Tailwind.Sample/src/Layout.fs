module Layout

open Feliz

let page (content:ReactElement list) =
  Html.div [
    prop.className "grid grid-cols-12 pt-6"
    prop.children [
      Html.div [ prop.className "col-span-2" ]
      Html.div [
        prop.className "col-span-8"
        prop.children content
      ]
      Html.div [ prop.className "col-span-2" ]      
    ]
  ]
  
let panel (content:ReactElement list) =
  Html.div [
    prop.className "bg-white rounded-lg p-6 shadow"
    prop.children content
  ]