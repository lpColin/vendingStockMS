const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export async function request(path, options = {}) {
  const response = await fetch(BASE_URL + path, {
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  })
  const payload = await response.json().catch(() => null)
  if (!response.ok || payload?.code !== 200) {
    throw new Error(payload?.message ?? ('请求失败（HTTP ' + response.status + '）'))
  }
  return payload.data
}

const query = (params) => new URLSearchParams(params).toString()

export const api = {
  health: () => request('/health'),
  overview: () => request('/api/v1/dashboard/overview'),
  rawProducts: (params = {}) => request('/api/v1/product-raw/list?' + query(params)),
  syncLogs: (params = {}) => request('/api/v1/sync-task/log/list?' + query(params)),
  warehouseStocks: (params = {}) => request('/api/v1/warehouse-stock/list?' + query(params)),
  machines: (params = {}) => request('/api/v1/vending-machine/list?' + query(params)),
  machine: (id) => request('/api/v1/vending-machine/' + id),
  createMachine: (data) => request('/api/v1/vending-machine', { method: 'POST', body: JSON.stringify(data) }),
  updateMachine: (id, data) => request('/api/v1/vending-machine/' + id, { method: 'PUT', body: JSON.stringify(data) }),
  deleteMachine: (id) => request('/api/v1/vending-machine/' + id, { method: 'DELETE' }),
  importExternalMachines: (payload) => request('/api/v1/vending-machine/import-external', { method: 'POST', body: JSON.stringify({ payload }) }),
  triggerGoodsSync: () => request('/api/v1/sync-task/goods/trigger', { method: 'POST' }),
}
