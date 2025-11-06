(function(){
  const inputSel='#aiInput', sendSel='#aiSend', outSel='#aiOutput';
  function token(){ return sessionStorage.getItem('ecn.jwt')||''; }
  async function ask(q){
    const r=await fetch('api/ai/ask',{method:'POST',headers:{'Content-Type':'application/json','Authorization':'Bearer '+token()},body:JSON.stringify({question:q})});
    if(!r.ok) throw new Error(await r.text());
    return (await r.json()).answer;
  }
  function wire(){
    const i=document.querySelector(inputSel), b=document.querySelector(sendSel), o=document.querySelector(outSel);
    if(!i||!b||!o) return;
    async function run(){ b.disabled=true; const t=b.textContent; b.textContent='...';
      try{ o.textContent=await ask(i.value.trim()); }catch(e){ o.textContent='AI error: '+(e.message||e); }
      finally{ b.disabled=false; b.textContent=t; } }
    b.addEventListener('click', run);
    i.addEventListener('keydown', e=>{ if(e.key==='Enter'&&!e.shiftKey){ e.preventDefault(); run(); } });
  }
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded', wire); else wire();
})();