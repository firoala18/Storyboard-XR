// wwwroot/js/copyPrompt.js
export async function copyPromptHtml(html, justReturn = false) {

    /* 1️⃣  HTML -> Plain-Text ------------------------------------------------ */
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    tmp.querySelectorAll('br').forEach(br => br.replaceWith('\n'));
    tmp.querySelectorAll('p,div').forEach(el => el.append('\n'));
    let text = tmp.textContent.replace(/\n{3,}/g, '\n\n').trim();

    /* 2️⃣  iPad / iPhone Spezialbehandlung ---------------------------------- */
    const isiPad = /ipad/i.test(navigator.userAgent) ||
                        (/macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
     const isiPhone = /iphone|ipod/i.test(navigator.userAgent);
    
         if (isiPad || isiPhone) {
                 /* show clipboard in DevTools – handy while testing                    */
                     // navigator.clipboard.readText().then(t => console.log('CLIP:', t));
                
                    const LS = '\u2028';   // “LINE SEPARATOR”  – looks like \n to the user
                 const WJ = '\u2060';   // “WORD-JOINER”     – zero-width, breaks URL test
            
                     /* a) normalise line breaks                                            */
                    text = text.replace(/\r?\n/g, LS);
            
                     /* b) prepend “#” to the four critical labels ***ONLY ON iPad/iPhone*** */
                     text = text.replace(
                           /(?<=^|\u2028)(Title|Thema|Ziele|Beschreibung):/gi,
                           (_, lbl) => `#${lbl}:`
                     );
            
                     /* c) for every remaining ASCII-only label add the WORD-JOINER         */
                    text = text.replace(
                           /(?<=^|\u2028)([A-Za-z0-9.+-]+):/g,
                           (_, label) => `${label}${WJ}:`
                     );
             }

    /* Wenn die Funktion nur den bereinigten Text liefern soll … */
    if (justReturn) return text;

    /* 3️⃣  Clipboard – modern API + Fallback ------------------------------- */
    try {
        await navigator.clipboard.writeText(text);
    } catch {
        const ta = Object.assign(document.createElement('textarea'), {
            value: text,
            style: 'position:fixed;top:-9999px;opacity:0'
        });
        document.body.appendChild(ta);
        ta.select(); document.execCommand('copy'); ta.remove();
    }

    /* 4️⃣  Toast-Feedback (falls vorhanden) --------------------------------- */
    const toastEl = document.getElementById('copy-toast');
    toastEl && bootstrap.Toast
        .getOrCreateInstance(toastEl, { delay: 2500 })
        .show();
}
