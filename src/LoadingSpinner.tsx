import React from 'react'
// @ts-ignore
import loadingIcon from './card-backgrounds/staff.png'

export function LoadingSpinner({isLoading}: {isLoading: boolean}) {

  return (
    <img className={isLoading ? 'loadingAnimation loadingIcon' : 'loadingIcon'} src={loadingIcon} />
  )
}