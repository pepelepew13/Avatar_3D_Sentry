window.EditorHelpers = (function(){
  function relativeTop(btnEl, railEl){
    if(!btnEl || !railEl) return 8;
    const br = btnEl.getBoundingClientRect();
    const rr = railEl.getBoundingClientRect();
    // top relativo al rail + scroll interno del rail
    const top = (br.top - rr.top) + railEl.scrollTop;
    return Math.max(8, Math.round(top));
  }

  // (opcional) seguir al botón mientras haces scroll/redimensionas
  function attachFollow(railEl, btnEl, subSelector){
    const sub = document.querySelector(subSelector);
    if(!railEl || !btnEl || !sub) return () => {};
    const update = () => {
      sub.style.top = relativeTop(btnEl, railEl) + "px";
    };
    railEl.addEventListener('scroll', update, {passive:true});
    window.addEventListener('resize', update, {passive:true});
    update();
    return () => {
      railEl.removeEventListener('scroll', update);
      window.removeEventListener('resize', update);
    };
  }

  return { relativeTop, attachFollow };
})();
