
/**
 * ECN AI Client — drop-in script
 * Usage:
 *  1) Include in ecn.html: <script src="ecn_ai_client.js"></script>
 *  2) Optional custom hooks:
 *     <input data-ai-input> <button data-ai-send>Ask</button> <pre data-ai-output></pre>
 *     If not provided, a floating widget is injected.
 */
(function(){
  const token = () => sessionStorage.getItem('ecn.jwt') || '';
  async function ask(question){
    const t = token();
    if(!t) throw new Error("Missing token. Please login again.");
    const r = await fetch('api/ai/ask', {
      method: 'POST',
      headers: {'Content-Type':'application/json', Authorization: 'Bearer '+t},
      body: JSON.stringify({ question })
    });
    if(!r.ok){
      const txt = await r.text().catch(()=>'');
      throw new Error(`AI HTTP ${r.status}: ${txt||r.statusText}`);
    }
    const data = await r.json();
    return data && (data.answer || JSON.stringify(data));
  }
  function wireCustom(){
    const input = document.querySelector('[data-ai-input]');
    const send = document.querySelector('[data-ai-send]');
    const out  = document.querySelector('[data-ai-output]');
    if(!(input && send && out)) return false;
    const setBusy = (b)=>{ send.disabled=b; send.textContent=b?'...':'Ask'; };
    send.addEventListener('click', async ()=>{
      const q = input.value.trim();
      if(!q) return;
      setBusy(true);
      try{ out.textContent = await ask(q); }
      catch(e){ out.textContent = 'AI error: '+ (e.message||e); }
      finally{ setBusy(false); }
    });
    return true;
  }
  function injectWidget(){
    const wrap = document.createElement('div');
    wrap.style = 'position:fixed;right:16px;bottom:16px;width:360px;max-width:90vw;background:#111;color:#fff;border-radius:14px;box-shadow:0 10px 30px rgba(0,0,0,.35);z-index:99999;padding:12px';
    wrap.innerHTML = `
      <div style="display:flex;gap:8px;align-items:center;justify-content:space-between;margin-bottom:6px">
        <strong>ECN AI Advisor</strong>
        <button data-close style="background:#333;border:0;border-radius:9px;color:#fff;padding:6px 8px;cursor:pointer">×</button>
      </div>
      <textarea data-q rows="3" placeholder="Ask about ECN (ECN No, Model, Before/After, policy...)" style="width:100%;border-radius:10px;border:0;padding:8px;color:#111"></textarea>
      <div style="display:flex;gap:8px;margin-top:8px">
        <button data-go style="flex:0 0 auto;background:#00d084;color:#111;border:0;border-radius:10px;padding:8px 12px;cursor:pointer">Ask</button>
        <button data-clear style="flex:0 0 auto;background:#444;color:#fff;border:0;border-radius:10px;padding:8px 12px;cursor:pointer">Clear</button>
      </div>
      <pre data-a style="margin-top:8px;white-space:pre-wrap;background:#0b0b0b;padding:8px;border-radius:10px;max-height:45vh;overflow:auto"></pre>
    `;
    const btn = wrap.querySelector('[data-go]');
    const q   = wrap.querySelector('[data-q]');
    const a   = wrap.querySelector('[data-a]');
    const clear = wrap.querySelector('[data-clear]');
    const close = wrap.querySelector('[data-close]');
    btn.addEventListener('click', async ()=>{
      const text = q.value.trim(); if(!text) return;
      btn.disabled = true; btn.textContent = '...';
      try{ a.textContent = await ask(text); }
      catch(e){ a.textContent = 'AI error: '+ (e.message||e); }
      finally{ btn.disabled=false; btn.textContent='Ask'; }
    });
    clear.addEventListener('click', ()=>{ a.textContent=''; q.value=''; });
    close.addEventListener('click', ()=>{ wrap.remove(); });
    document.body.appendChild(wrap);
  }
  window.addEventListener('DOMContentLoaded', ()=>{
    if(!wireCustom()) injectWidget();
  });
  window.ECN_AI = { ask };
})();
