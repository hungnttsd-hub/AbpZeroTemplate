export function Pip({ className = '', friend = 'pip' }: { className?: string; friend?: string }) {
  const color = friend === 'poki' ? '#faf9ef' : friend === 'momo' ? '#a6c99b' : friend === 'lulu' ? '#eed6d7' : '#ffd267';
  return <svg className={className} viewBox="0 0 240 240" fill="none" aria-hidden="true"><ellipse cx="122" cy="216" rx="69" ry="12" fill="#365f46" opacity=".12"/>
    {friend === 'lulu' ? <g fill={color} stroke="#c29ea7" strokeWidth="4"><ellipse cx="89" cy="51" rx="20" ry="45"/><ellipse cx="145" cy="48" rx="20" ry="45"/></g> : friend === 'poki' ? <g fill="#475851"><circle cx="63" cy="62" r="27"/><circle cx="171" cy="62" r="27"/></g> : <path d="m110 56-6-24 19 10 17-12 3 27" fill={color}/>}
    <path d="M164 133q51-21 44 20-7 29-40 20" fill="#d49f44"/><path d="M76 153q-47 15-34 41 11 13 42-14" fill={color}/>
    <ellipse cx="123" cy="135" rx="73" ry="78" fill={color}/><ellipse cx="126" cy="162" rx="48" ry="40" fill="#fff4cc"/>
    {friend === 'poki' && <g fill="#475851"><ellipse cx="92" cy="103" rx="20" ry="25" transform="rotate(20 92 103)"/><ellipse cx="151" cy="103" rx="20" ry="25" transform="rotate(-20 151 103)"/></g>}
    <g fill="#374b3a"><ellipse cx="95" cy="103" rx="7" ry="10"/><ellipse cx="150" cy="103" rx="7" ry="10"/></g><g fill="white"><circle cx="97" cy="100" r="2.5"/><circle cx="152" cy="100" r="2.5"/></g>
    <ellipse cx="77" cy="122" rx="12" ry="7" fill="#edaa76"/><ellipse cx="165" cy="122" rx="12" ry="7" fill="#edaa76"/>
    <path d="m111 123 26 0-13 15Z" fill="#dc8548"/><path d="M81 151q42 18 81-1l-16 25-29-8-20 12Z" fill="#79a990"/>
    <path d="M81 199q-18 16 5 17h25l-3-16m31 0-2 16h26q20-7-4-18" fill="#de9d53"/>
    <path d="M62 74q60-24 121 1l-6-16q-24-54-87-25L70 62" fill="#9cba83"/><path d="M56 78q64-17 133 1" stroke="#739267" strokeWidth="12" strokeLinecap="round"/>
    <path d="m128 45 11 11-11 11-11-11Z" fill="#fff7ce"/></svg>;
}
export function Island({ type = 0, className = '' }: { type?: number; className?: string }) {
  return <svg className={className} viewBox="0 0 640 390" fill="none" aria-hidden="true"><defs><linearGradient id={`land-${type}`} x1="300" y1="200" x2="300" y2="380" gradientUnits="userSpaceOnUse"><stop stopColor="#d3bc85"/><stop offset="1" stopColor="#9d986f"/></linearGradient></defs>
    <ellipse cx="328" cy="356" rx="200" ry="18" fill="#497d70" opacity=".1"/>
    <path d="M101 251q223-86 442-1l-59 69-69 10-76 47-85-39-93-9Z" fill={`url(#land-${type})`}/>
    <ellipse cx="322" cy="248" rx="225" ry="74" fill={type === 2 ? '#b8cd96' : '#99c78c'}/><ellipse cx="322" cy="236" rx="208" ry="61" fill={type === 1 ? '#acd393' : '#c1d99e'}/>
    <path d="M188 270q45-51 139-35t111-24" stroke="#f4e5b7" strokeWidth="29" strokeLinecap="round"/>
    {type === 0 && <><path d="M186 211a138 138 0 0 1 276 0" stroke="#eaa797" strokeWidth="22"/><path d="M210 211a114 114 0 0 1 228 0" stroke="#f3cd82" strokeWidth="22"/><path d="M234 211a90 90 0 0 1 180 0" stroke="#abd2aa" strokeWidth="22"/><path d="M257 211a67 67 0 0 1 134 0" stroke="#acd0db" strokeWidth="22"/>
      <g fill="#fffaf0"><ellipse cx="188" cy="209" rx="44" ry="21"/><circle cx="173" cy="194" r="25"/><circle cx="201" cy="197" r="21"/><ellipse cx="456" cy="209" rx="47" ry="20"/><circle cx="441" cy="190" r="26"/><circle cx="473" cy="197" r="20"/></g></>}
    {type === 1 && <><path d="M294 213q-42 45 20 70l-11 47 42 16 14-69q-53-36-12-68" fill="#92cbd2"/><path d="M310 210q-38 45 20 66l-5 65" stroke="#d9f0e5" strokeWidth="11"/><path d="M187 238V108m245 107V96" stroke="#9a9265" strokeWidth="20"/><g fill="#749e73"><path d="M187 115q-90-74-119 7 74-15 119-7Z"/><path d="M187 115q88-83 137-7-82-7-137 7Z"/><path d="M431 98q-75-69-112 4 64-5 112-4Z"/><path d="M431 98q79-72 121 5-72-11-121-5Z"/></g></>}
    {type === 2 && <><rect x="225" y="146" width="200" height="114" rx="9" fill="#fae6c4"/><path d="m196 154 130-101 127 101Z" fill="#cf9582"/><rect x="306" y="182" width="45" height="78" rx="22" fill="#a1bfb0"/><rect x="244" y="179" width="40" height="39" rx="5" fill="#badde1"/><rect x="370" y="179" width="36" height="39" rx="5" fill="#badde1"/><path d="M263 181v36m-16-18h34m107-18v36m-16-18h30" stroke="#fff7e7" strokeWidth="5"/><path d="M393 97V59h-25v19" fill="#c39b82"/></>}
    <g stroke="#718e61" strokeWidth="9" strokeLinecap="round"><path d="M147 248v-62m348 61v-62"/></g><g fill="#86b67b"><circle cx="147" cy="175" r="31"/><circle cx="495" cy="173" r="32"/></g>
    <g fill="#f8e7a5"><circle cx="194" cy="273" r="6"/><circle cx="453" cy="260" r="6"/><circle cx="241" cy="284" r="6"/></g><g fill="#eeafa8"><circle cx="184" cy="264" r="5"/><circle cx="459" cy="271" r="6"/></g>
    <path d="m508 110 5-13 5 13 14 5-14 5-5 13-5-13-14-5Z" fill="#edc875"/></svg>;
}
