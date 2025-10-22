const apiBase = window.location.origin + "/api/Station";

async function fetchList() {
  const q = document.getElementById('q').value;
  const location = document.getElementById('location').value;
  const minPower = document.getElementById('minPower').value;
  const params = new URLSearchParams();
  if (q) params.append('q', q);
  if (location) params.append('location', location);
  if (minPower) params.append('minPower', minPower);
  params.append('page', '1');
  params.append('pageSize', '100');

  const res = await fetch(apiBase + "?" + params.toString());
  if (!res.ok) { alert("Error fetching list: " + res.status); return; }
  const data = await res.json();
  renderTable(data.items || []);
}

function renderTable(items) {
  const tbody = document.querySelector('#tbl tbody');
  tbody.innerHTML = '';
  for (const s of items) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${s.id}</td>
      <td>${escapeHtml(s.name)}</td>
      <td>${escapeHtml(s.location)}</td>
      <td>${s.power}</td>
      <td>${s.status || ''}</td>
      <td>${new Date(s.createdAt).toLocaleString()}</td>
      <td>
        <button class="btn btn-sm btn-primary btn-edit" data-id="${s.id}">Sửa</button>
        <button class="btn btn-sm btn-danger btn-delete" data-id="${s.id}">Xóa</button>
        <button class="btn btn-sm btn-warning btn-toggle" data-id="${s.id}" data-status="${s.status === 'Online' ? 'Offline' : 'Online'}">${s.status === 'Online' ? 'Off' : 'On'}</button>
      </td>`;
    tbody.appendChild(tr);
  }
  attachRowEvents();
}

function escapeHtml(text) {
  if (!text) return '';
  return text.replace(/[&<>"']/g, (m) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":"&#039;"}[m]));
}

function attachRowEvents() {
  document.querySelectorAll('.btn-delete').forEach(b => b.onclick = async (e) => {
    const id = e.target.dataset.id;
    if (!confirm("Xác nhận xóa ID " + id + "?")) return;
    const res = await fetch(apiBase + "/" + id, { method: 'DELETE' });
    if (res.ok) fetchList(); else alert("Xóa lỗi: " + res.status);
  });

  document.querySelectorAll('.btn-toggle').forEach(b => b.onclick = async (e) => {
    const id = e.target.dataset.id;
    const status = e.target.dataset.status;
    const res = await fetch(apiBase + "/" + id + "/status", {
      method: 'PATCH',
      headers: {'Content-Type':'application/json'},
      body: JSON.stringify({ status })
    });
    if (res.ok) fetchList(); else alert("Update status lỗi: " + res.status);
  });

  document.querySelectorAll('.btn-edit').forEach(b => b.onclick = async (e) => {
    const id = e.target.dataset.id;
    const name = prompt("Name?");
    if (name === null) return;
    const location = prompt("Location?");
    if (location === null) return;
    const power = prompt("Power kW?");
    if (power === null) return;
    const body = { name, location, power: Number(power), status: "Offline" };
    const res = await fetch(apiBase + "/" + id, {
      method: 'PUT',
      headers: {'Content-Type':'application/json'},
      body: JSON.stringify(body)
    });
    if (res.ok) fetchList(); else alert("Update lỗi: " + res.status);
  });
}

document.getElementById('btnSearch').onclick = (e) => { e.preventDefault(); fetchList(); };
document.getElementById('btnReload').onclick = (e) => { document.getElementById('q').value=''; document.getElementById('location').value=''; document.getElementById('minPower').value=''; fetchList(); };

document.getElementById('createForm').onsubmit = async (e) => {
  e.preventDefault();
  const name = document.getElementById('name').value;
  const loc = document.getElementById('loc').value;
  const power = Number(document.getElementById('power').value || 0);
  const status = document.getElementById('status').value;
  const res = await fetch(apiBase, {
    method: 'POST',
    headers: {'Content-Type':'application/json'},
    body: JSON.stringify({ name, location: loc, power, status })
  });
  if (res.ok) {
    var modalEl = document.getElementById('createModal');
    var modal = bootstrap.Modal.getInstance(modalEl);
    modal.hide();
    document.getElementById('createForm').reset();
    fetchList();
  } else {
    alert("Thêm lỗi: " + res.status);
  }
};

// initial load
fetchList();
