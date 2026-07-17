<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Box, Connection, DataBoard, Goods, House, Refresh, Van } from '@element-plus/icons-vue'
import { api } from './services/api'

const active = ref('dashboard')
const loading = ref(false)
const syncRunning = ref(false)
const machineImportVisible = ref(false)
const machineImportRunning = ref(false)
const machineImportPayload = ref('')
const machineDialogVisible = ref(false)
const machineSaving = ref(false)
const machineEditingId = ref(null)
const machineKeyword = ref('')
const machinePage = ref(1)
const machineTotal = ref(0)
const machineForm = ref({ machineCode: '', address: '', onlineTime: toLocalDateTime(new Date()), status: 0, manager: '', managerPhone: '' })
const machineFormRef = ref()
const machineRules = { machineCode: [{ required: true, message: '请输入机器编码', trigger: 'blur' }], address: [{ required: true, message: '请输入设备地址', trigger: 'blur' }], onlineTime: [{ required: true, message: '请选择上线时间', trigger: 'change' }], manager: [{ required: true, message: '请输入负责人', trigger: 'blur' }], managerPhone: [{ required: true, message: '请输入负责人电话', trigger: 'blur' }] }
const apiStatus = ref('检查中')
const overview = ref({ warehouseCount: 0, machineCount: 0, pendingDeliveryCount: 0, urgentRestockCount: 0, todaySalesAmount: 0, lastGoodsSync: null })
const rawProducts = ref([])
const syncLogs = ref([])
const stocks = ref([])
const machines = ref([])
const rawKeyword = ref('')
const rawPage = ref(1)
const rawTotal = ref(0)
const pageSize = 20

const menus = [
  { key: 'dashboard', label: '工作台', icon: DataBoard },
  { key: 'raw-products', label: '原始商品数据', icon: Goods },
  { key: 'warehouse-stock', label: '仓库库存', icon: Box },
  { key: 'machines', label: '贩卖机', icon: House },
  { key: 'sync-logs', label: '同步记录', icon: Connection },
]
const currentTitle = computed(() => menus.find((item) => item.key === active.value)?.label ?? '运营管理')
const formatMoney = (value) => '¥' + Number(value ?? 0).toFixed(2)
const formatDate = (value) => value ? new Date(value).toLocaleString('zh-CN', { hour12: false }) : '-'
const statusName = (value) => ({ 0: '运行中', 1: '成功', 2: '失败' }[value] ?? '未知')
const statusType = (value) => ({ 0: 'warning', 1: 'success', 2: 'danger' }[value] ?? 'info')
const machineStatus = (value) => ({ 0: '正常', 1: '故障', 2: '维修中' }[value] ?? '未知')
const machineType = (value) => ({ 0: 'success', 1: 'danger', 2: 'warning' }[value] ?? 'info')
function toLocalDateTime(value) { const date = new Date(value); const offset = date.getTimezoneOffset() * 60000; return new Date(date.getTime() - offset).toISOString().slice(0, 19) }

async function loadDashboard() { overview.value = await api.overview() }
async function loadRawProducts() { const data = await api.rawProducts({ page: rawPage.value, pageSize, keyword: rawKeyword.value }); rawProducts.value = data.list; rawTotal.value = data.total }
async function loadStocks() { const data = await api.warehouseStocks({ page: 1, pageSize: 50 }); stocks.value = data.list }
async function loadMachines() { const data = await api.machines({ page: machinePage.value, pageSize, keyword: machineKeyword.value }); machines.value = data.list; machineTotal.value = data.total }
async function loadSyncLogs() { const data = await api.syncLogs({ page: 1, pageSize: 30 }); syncLogs.value = data.list }
async function refreshCurrent() {
  loading.value = true
  try {
    if (active.value === 'dashboard') await loadDashboard()
    if (active.value === 'raw-products') await loadRawProducts()
    if (active.value === 'warehouse-stock') await loadStocks()
    if (active.value === 'machines') await loadMachines()
    if (active.value === 'sync-logs') await loadSyncLogs()
  } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
}
async function changePage(key) { active.value = key; await refreshCurrent() }
async function searchRawProducts() { rawPage.value = 1; await refreshCurrent() }
async function changeRawPage(page) { rawPage.value = page; await refreshCurrent() }
function newMachineForm() { return { machineCode: '', address: '', onlineTime: toLocalDateTime(new Date()), status: 0, manager: '', managerPhone: '' } }
function openCreateMachine() { machineEditingId.value = null; machineForm.value = newMachineForm(); machineDialogVisible.value = true }
function openEditMachine(row) { machineEditingId.value = row.id; machineForm.value = { machineCode: row.machineCode, address: row.address, onlineTime: toLocalDateTime(row.onlineTime), status: row.status, manager: row.manager, managerPhone: row.managerPhone }; machineDialogVisible.value = true }
async function saveMachine() {
  if (!machineFormRef.value) return
  const valid = await machineFormRef.value.validate().catch(() => false)
  if (!valid) return
  machineSaving.value = true
  try {
    const data = { ...machineForm.value, onlineTime: new Date(machineForm.value.onlineTime).toISOString() }
    if (machineEditingId.value) await api.updateMachine(machineEditingId.value, data); else await api.createMachine(data)
    ElMessage.success(machineEditingId.value ? '贩卖机已更新' : '贩卖机已新增')
    machineDialogVisible.value = false
    await Promise.all([loadMachines(), loadDashboard()])
  } catch (error) { ElMessage.error(error.message) } finally { machineSaving.value = false }
}
async function removeMachine(row) {
  try {
    await ElMessageBox.confirm('删除后将无法恢复。若该机器已有关联商品或配货单，系统可能拒绝删除。', '确认删除 ' + row.machineCode, { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' })
    await api.deleteMachine(row.id)
    ElMessage.success('贩卖机已删除')
    if (machines.value.length === 1 && machinePage.value > 1) machinePage.value -= 1
    await Promise.all([loadMachines(), loadDashboard()])
  } catch (error) { if (error !== 'cancel') ElMessage.error(error.message) }
}
async function searchMachines() { machinePage.value = 1; await loadMachines() }
async function changeMachinePage(page) { machinePage.value = page; await loadMachines() }
async function openMachineImport() { machineImportPayload.value = ''; machineImportVisible.value = true }
async function importMachines() {
  if (!machineImportPayload.value.trim()) { ElMessage.warning('请粘贴机器数据 JSON'); return }
  machineImportRunning.value = true
  try {
    const result = await api.importExternalMachines(machineImportPayload.value)
    ElMessage.success(`导入完成：新增 ${result.createdCount} 台，更新 ${result.updatedCount} 台`)
    machineImportVisible.value = false
    await Promise.all([loadMachines(), loadDashboard()])
  } catch (error) { ElMessage.error(error.message) } finally { machineImportRunning.value = false }
}
async function triggerSync() {
  await ElMessageBox.confirm('将立即调用优宝商品接口并更新商品原始数据。该过程约需数秒。', '确认同步', { confirmButtonText: '开始同步', cancelButtonText: '取消', type: 'warning' })
  syncRunning.value = true
  try { const result = await api.triggerGoodsSync(); ElMessage.success(`同步完成：成功 ${result.successCount} 条`); await Promise.all([loadRawProducts(), loadSyncLogs(), loadDashboard()]) }
  catch (error) { if (error !== "cancel") ElMessage.error(error.message) } finally { syncRunning.value = false }
}
async function initialize() {
  try { await api.health(); apiStatus.value = "API 已连接"; await Promise.all([loadDashboard(), loadRawProducts(), loadSyncLogs()]) }
  catch (error) { apiStatus.value = "API 不可用"; ElMessage.error("无法连接后端：" + error.message) }
}
onMounted(initialize)
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand"><span class="brand-mark">VS</span><div><b>VendingStockMS</b><small>货柜仓储管理</small></div></div>
      <nav class="main-nav"><button v-for="item in menus" :key="item.key" :class="{ active: active === item.key }" @click="changePage(item.key)"><el-icon><component :is="item.icon" /></el-icon><span>{{ item.label }}</span></button></nav>
      <div class="connection"><span class="connection-dot" :class="{ offline: apiStatus !== 'API 已连接' }"></span><span>{{ apiStatus }}</span><small>本地安全通道</small></div>
    </aside>
    <main class="main-content">
      <header class="topbar"><div><span class="breadcrumb">运营管理</span><h1>{{ currentTitle }}</h1></div><div class="topbar-actions"><el-button :icon="Refresh" circle :loading="loading" @click="refreshCurrent" title="刷新当前页面"/><el-button v-if="active === 'raw-products'" type="primary" :icon="Refresh" :loading="syncRunning" @click="triggerSync">同步基础商品数据</el-button><el-button v-if="active === 'machines'" :icon="Van" @click="openMachineImport">导入机器数据</el-button><el-button v-if="active === 'machines'" type="primary" :icon="House" @click="openCreateMachine">新增贩卖机</el-button></div></header>
      <section v-loading="loading" class="content-view">
        <template v-if="active === 'dashboard'">
          <div class="page-intro"><div><h2>运营概览</h2><p>汇总当前仓储、设备、配送与销售数据。</p></div><span v-if="overview.lastGoodsSync" class="last-sync">商品数据最近同步：{{ formatDate(overview.lastGoodsSync) }}</span></div>
          <div class="metrics-grid"><article><span>仓库数量</span><strong>{{ overview.warehouseCount }}</strong><small>个仓库</small></article><article><span>贩卖机数量</span><strong>{{ overview.machineCount }}</strong><small>台设备</small></article><article><span>待处理配货单</span><strong>{{ overview.pendingDeliveryCount }}</strong><small>张订单</small></article><article><span>今日销售总额</span><strong>{{ formatMoney(overview.todaySalesAmount) }}</strong><small>当日累计</small></article></div>
          <div class="dashboard-panels"><article class="panel"><div class="panel-title"><h3>需要关注</h3><el-tag type="warning">{{ overview.urgentRestockCount }} 项紧急补货</el-tag></div><div class="empty-note" v-if="overview.machineCount === 0">尚未创建贩卖机。先在“贩卖机”模块登记设备，即可开始计算补货建议。</div><div class="empty-note" v-else>补货数量和库存预警将根据实际设备商品数据自动计算。</div></article><article class="panel"><div class="panel-title"><h3>最近同步</h3><button class="text-button" @click="changePage('sync-logs')">查看记录</button></div><div v-if="syncLogs.length" class="mini-log" v-for="item in syncLogs.slice(0, 3)" :key="item.id"><div><b>{{ item.taskType }}</b><span>{{ formatDate(item.finishedAt || item.startedAt) }}</span></div><el-tag :type="statusType(item.status)" size="small">{{ statusName(item.status) }}</el-tag></div><div v-else class="empty-note">暂无同步记录。</div></article></div>
        </template>
        <template v-else-if="active === 'raw-products'">
          <div class="page-intro"><div><h2>商品原始数据</h2><p>来自优宝批发接口的 SKU 基础数据，系统每日 04:00 自动同步。</p></div></div>
          <div class="filter-bar"><el-input v-model="rawKeyword" clearable placeholder="搜索商品名称" @keyup.enter="searchRawProducts" @clear="searchRawProducts"><template #append><el-button @click="searchRawProducts">搜索</el-button></template></el-input><span>共 {{ rawTotal }} 条</span></div>
          <div class="data-card"><el-table :data="rawProducts" empty-text="暂无商品原始数据"><el-table-column label="商品" min-width="260"><template #default="{ row }"><div class="product-cell"><el-image :src="row.imageUrl" fit="cover"><template #error><div class="image-fallback"><el-icon><Goods /></el-icon></div></template></el-image><div><b>{{ row.skuName }}</b><span>SKU：{{ row.skuId }}</span></div></div></template></el-table-column><el-table-column prop="brandName" label="品牌" width="120"/><el-table-column prop="categoryName1" label="一级分类" width="110"/><el-table-column prop="categoryName2" label="二级分类" width="130"/><el-table-column prop="specStr" label="规格" width="120"/><el-table-column label="箱价" width="105"><template #default="{ row }">{{ formatMoney(row.boxPrice) }}</template></el-table-column><el-table-column prop="stock" label="接口库存" width="100"/></el-table><div class="pager"><el-pagination background layout="total, prev, pager, next" :current-page="rawPage" :page-size="pageSize" :total="rawTotal" @current-change="changeRawPage"/></div></div>
        </template>
        <template v-else-if="active === 'warehouse-stock'">
          <div class="page-intro"><div><h2>仓库库存</h2><p>按仓库和商品查看当前可用库存。</p></div></div>
          <div class="data-card"><el-table :data="stocks" empty-text="暂无仓库库存，请先创建仓库和基础商品后录入库存"><el-table-column prop="warehouseName" label="仓库" min-width="160"/><el-table-column prop="productName" label="商品" min-width="220"/><el-table-column prop="brandName" label="品牌" width="120"/><el-table-column prop="specName" label="规格" width="130"/><el-table-column prop="quantity" label="库存数量" width="120"/><el-table-column label="标签" min-width="150"><template #default="{ row }"><el-tag v-for="tag in row.tags || []" :key="tag" size="small" class="stock-tag">{{ tag }}</el-tag></template></el-table-column></el-table></div>
        </template>
        <template v-else-if="active === 'machines'">
          <div class="page-intro"><div><h2>贩卖机管理</h2><p>维护设备编码、点位、状态与负责人信息，也可导入机器点位数据。</p></div></div>
          <div class="filter-bar"><el-input v-model="machineKeyword" clearable placeholder="搜索机器编码或设备地址" @keyup.enter="searchMachines" @clear="searchMachines"><template #append><el-button @click="searchMachines">搜索</el-button></template></el-input><span>共 {{ machineTotal }} 台</span></div>
          <div class="data-card"><el-table :data="machines" empty-text="暂无贩卖机，请新增或导入机器数据"><el-table-column prop="machineCode" label="机器编码" width="150"/><el-table-column prop="address" label="设备地址" min-width="260"/><el-table-column label="状态" width="110"><template #default="{ row }"><el-tag :type="machineType(row.status)">{{ machineStatus(row.status) }}</el-tag></template></el-table-column><el-table-column prop="manager" label="负责人" width="120"/><el-table-column prop="managerPhone" label="联系电话" width="150"/><el-table-column label="上线时间" width="180"><template #default="{ row }">{{ formatDate(row.onlineTime) }}</template></el-table-column><el-table-column label="操作" width="150" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openEditMachine(row)">编辑</el-button><el-button link type="danger" @click="removeMachine(row)">删除</el-button></template></el-table-column></el-table><div class="pager"><el-pagination background layout="total, prev, pager, next" :current-page="machinePage" :page-size="pageSize" :total="machineTotal" @current-change="changeMachinePage"/></div></div>
        </template>
        <template v-else>
          <div class="page-intro"><div><h2>同步记录</h2><p>记录基础商品数据的自动和手动同步执行情况。</p></div></div>
          <div class="data-card"><el-table :data="syncLogs" empty-text="暂无同步记录"><el-table-column prop="id" label="ID" width="80"/><el-table-column prop="taskType" label="任务类型" width="150"/><el-table-column label="状态" width="110"><template #default="{ row }"><el-tag :type="statusType(row.status)">{{ statusName(row.status) }}</el-tag></template></el-table-column><el-table-column prop="totalCount" label="总数" width="100"/><el-table-column prop="successCount" label="成功" width="100"/><el-table-column prop="failedCount" label="失败" width="100"/><el-table-column label="完成时间" width="190"><template #default="{ row }">{{ formatDate(row.finishedAt) }}</template></el-table-column><el-table-column prop="errorMessage" label="错误信息" min-width="200"/></el-table></div>
        </template>
      </section>
    </main>
    <el-dialog v-model="machineDialogVisible" :title="machineEditingId ? '编辑贩卖机' : '新增贩卖机'" width="620px" :close-on-click-modal="false" destroy-on-close>
      <el-form ref="machineFormRef" :model="machineForm" :rules="machineRules" label-width="92px" class="machine-form">
        <div class="machine-form-grid">
          <el-form-item label="机器编码" prop="machineCode"><el-input v-model="machineForm.machineCode" placeholder="例如：VM001" /></el-form-item>
          <el-form-item label="设备状态" prop="status"><el-select v-model="machineForm.status"><el-option label="正常" :value="0"/><el-option label="故障" :value="1"/><el-option label="维修中" :value="2"/></el-select></el-form-item>
          <el-form-item class="machine-form-wide" label="设备地址" prop="address"><el-input v-model="machineForm.address" placeholder="填写设备所在点位或地址" /></el-form-item>
          <el-form-item label="负责人" prop="manager"><el-input v-model="machineForm.manager" placeholder="填写负责人姓名" /></el-form-item>
          <el-form-item label="负责人电话" prop="managerPhone"><el-input v-model="machineForm.managerPhone" placeholder="填写联系电话" /></el-form-item>
          <el-form-item class="machine-form-wide" label="上线时间" prop="onlineTime"><el-date-picker v-model="machineForm.onlineTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" placeholder="选择上线时间" /></el-form-item>
        </div>
      </el-form>
      <template #footer><el-button @click="machineDialogVisible = false">取消</el-button><el-button type="primary" :loading="machineSaving" @click="saveMachine">保存</el-button></template>
    </el-dialog>
    <el-dialog v-model="machineImportVisible" title="导入机器数据" width="680px" class="machine-import-dialog" :close-on-click-modal="false">
      <p class="dialog-hint">粘贴接口响应 JSON。系统仅使用 <code>body[].vmCode</code> 作为机器编码和 <code>body[].nodeName</code> 作为设备地址；已有机器只更新设备地址。</p>
      <el-input v-model="machineImportPayload" type="textarea" :rows="13" placeholder='{ "body": [{ "vmCode": "51202125", "nodeName": "设备点位名称" }] }' />
      <template #footer><el-button @click="machineImportVisible = false">取消</el-button><el-button type="primary" :loading="machineImportRunning" @click="importMachines">确认导入</el-button></template>
    </el-dialog>
    <nav class="mobile-nav"><button v-for="item in menus.slice(0, 4)" :key="item.key" :class="{ active: active === item.key }" @click="changePage(item.key)"><el-icon><component :is="item.icon" /></el-icon><span>{{ item.label }}</span></button></nav>
  </div>
</template>