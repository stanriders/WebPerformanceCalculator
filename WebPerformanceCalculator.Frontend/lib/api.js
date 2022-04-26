const apiBase = 'https://pp.stanr.info/api'
//const apiBase = 'http://localhost:5001/api'

export default async function api (endpoint, options) {
  const response = await fetch(`${apiBase}${endpoint}`, {
    /*credentials: 'include',*/
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    ...options
  })

  if (response.status === 204) {
    return null
  }

  const responseData = await response.json()

  if (response.ok) {
    return responseData
  }

  throw responseData
}

export async function getPlayer(id) {
  const res = await fetch(apiBase+'/player/'+id)
  return res.json()
}

export async function getBuild() {
  const res = await fetch(apiBase+'/version')
  return res.json()
}

export async function getQueue() {
  const res = await fetch(apiBase+'/queue')
  return res.json()
}

export async function addToQueue(player) {
  return await api(`/queue/${player}`, {method: 'POST'})
}