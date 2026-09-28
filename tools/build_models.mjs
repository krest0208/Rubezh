// Original, editable vector models. Each variant changes geometry and silhouette.
// Units face right. No external assets, recolour-only variants, or bitmap dependencies.
import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve(import.meta.dirname,'..');
const out=path.join(root,'native/Game/Art/Models'); fs.mkdirSync(out,{recursive:true});
const dark='#192321', edge='#131c1a', steel='#60726d', light='#adba99', brass='#d5ad5b', olive='#74805a', wood='#826447';
const rect=(x,y,w,h,c,r=1)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${c}" stroke="${edge}" stroke-width="1.5"/>`;
const circle=(x,y,r,c)=>`<circle cx="${x}" cy="${y}" r="${r}" fill="${c}" stroke="${edge}" stroke-width="1.4"/>`;
const poly=(points,c)=>`<polygon points="${points}" fill="${c}" stroke="${edge}" stroke-width="1.6" stroke-linejoin="round"/>`;
const line=(x,y,a,b,c,w=2)=>`<path d="M${x},${y}L${a},${b}" stroke="${c}" stroke-width="${w}" stroke-linecap="round"/>`;
const bolts=(x,y,w,h)=>[[x,y],[x+w,y],[x,y+h],[x+w,y+h]].map(([a,b])=>circle(a,b,1.6,light)).join('');
const barrel=(x,y,length,width=6)=>rect(x,y-width/2,length,width,steel)+rect(x+length-6,y-width/2-2,8,width+4,dark)+line(x+2,y-width/2+1,x+length-8,y-width/2+1,light,1);
function tracks(x,y,w,h) {return [-1,1].map(s=>{let a=y+s*(h/2+5);return rect(x,a-6,w,12,dark,4)+Array.from({length:Math.floor(w/6)},(_,i)=>line(x+3+i*6,a-4,x+3+i*6,a+4,steel,2)).join('');}).join('');}
function wheels(x,y,w,h) {return [-1,1].map(s=>[0,w-12].map(a=>rect(x+a,y+s*(h/2+5)-5,12,10,dark,3)+line(x+a+2,y+s*(h/2+5),x+a+10,y+s*(h/2+5),steel,2)).join('')).join('');}
function bags() {return Array.from({length:8},(_,i)=>{let a=(i/7)*Math.PI*1.65+.5;return `<g transform="translate(${60+Math.cos(a)*29} ${64+Math.sin(a)*29}) rotate(${a*180/Math.PI})">${rect(-6,-10,12,20,'#a59c76',5)}${line(0,-8,0,8,'#786e50',1)}</g>`;}).join('');}
function tripod() {return line(60,64,33,40,steel,6)+line(60,64,33,88,steel,6)+line(60,64,77,93,steel,6)+circle(60,64,16,olive);}
function bunker() {return poly('21,38 40,22 83,25 95,46 95,81 81,101 38,99 21,80','#777a6a')+poly('28,41 43,30 79,32 86,48 86,80 77,91 42,90 28,77','#a1a38d')+rect(73,45,17,38,dark)+bolts(34,38,44,44);}
function crew(x,y,variant=0) {return rect(x-10,y-8,19,16,variant?'#8d9187':olive,6)+circle(x+3,y,7,light)+circle(x+5,y,6,variant?steel:olive)+line(x-8,y+7,x-15,y+10,wood,4);}
function tower(kind,v) {
  let s='';
  if(kind==='mg') {
    s=v===0?bags()+tripod():v===1?bunker():tracks(31,64,56,45)+rect(29,41,56,46,steel,8)+circle(58,64,19,olive);
    s+=v===2?barrel(64,56,45,7)+barrel(64,73,45,7)+rect(46,46,28,37,olive,5):barrel(57,64,49,7)+rect(42,55,30,18,olive,3)+rect(45,76,21,11,wood);
  } else if(kind==='rifle') {
    s=v===0?bags()+crew(41,61)+barrel(53,61,55,3):v===1?rect(30,34,48,58,wood,2)+line(23,27,81,97,wood,5)+line(23,97,81,27,wood,5)+rect(32,36,44,54,'#a59169')+crew(45,66,1)+barrel(53,66,58,4)+circle(68,55,4,brass):poly('28,35 66,28 85,48 84,82 60,99 26,90',steel)+crew(46,48)+crew(45,81)+barrel(54,47,52,4)+barrel(53,80,47,4)+rect(27,57,20,13,dark);
  } else if(kind==='mortar') {
    s=v===0?bags()+circle(61,64,24,steel)+line(47,81,78,42,light,5)+barrel(51,64,45,13):v===1?tracks(24,64,62,48)+rect(23,40,64,48,olive,8)+circle(57,64,22,steel)+barrel(54,64,47,17)+rect(30,43,14,40,wood):rect(25,35,69,59,steel,8)+wheels(28,64,62,59)+barrel(44,48,55,12)+barrel(44,79,55,12)+rect(24,49,20,30,olive)+bolts(30,40,55,46);
  } else if(kind==='cannon') {
    s=v===0?wheels(41,64,39,55)+line(50,64,21,36,olive,7)+line(50,64,21,92,olive,7)+poly('56,34 72,39 72,89 56,94',olive)+circle(60,64,13,steel)+barrel(64,64,53,9):v===1?tracks(23,64,70,49)+poly('24,39 66,33 92,44 93,86 67,94 24,86',steel)+poly('43,43 78,45 88,64 78,83 43,86',olive)+barrel(64,64,56,12)+bolts(48,47,27,34):bunker()+rect(38,40,42,49,olive,5)+barrel(63,51,55,9)+barrel(63,78,55,9)+circle(48,64,9,steel);
  } else if(kind==='sapper') {
    s=v===0?bags()+rect(37,46,32,36,wood)+circle(47,58,7,steel)+circle(59,71,7,steel)+crew(78,89):v===1?tracks(29,64,59,40)+rect(30,44,54,40,brass,5)+poly('87,29 106,34 112,64 106,94 87,99',steel)+rect(62,47,24,34,olive)+line(40,43,25,23,brass,5):rect(28,38,44,52,olive,4)+line(39,40,34,10,steel,3)+circle(35,10,3,brass)+circle(85,45,14,steel)+circle(85,82,14,steel)+line(62,50,83,46,brass,3)+line(62,78,83,82,brass,3)+bolts(34,44,30,40);
    if(v===0) s+=line(75,39,99,54,steel,6)+line(75,54,99,39,steel,6);
  } else if(kind==='rocket') {
    s=v===0?wheels(20,64,78,40)+rect(20,43,61,42,olive,2)+rect(79,44,23,40,steel,5)+rect(85,48,7,32,'#b3c5bd'):v===1?tracks(19,64,82,53)+poly('20,38 78,34 100,48 100,80 78,95 20,90',steel):poly('18,31 90,31 102,44 102,84 90,97 18,97',olive)+line(16,27,104,101,steel,5)+line(16,101,104,27,steel,5);
    let n=v===2?6:4;
    for(let i=0;i<n;i++) s+=barrel(28,42+i*(44/(n-1)),v===1?74:57,v===2?6:8)+poly(`${v===1?104:87},${42+i*(44/(n-1))} ${v===1?96:79},${38+i*(44/(n-1))} ${v===1?96:79},${46+i*(44/(n-1))}`,brass);
  }
  return s;
}
function enemy(kind,v) {
  if(kind==='infantry') return v===0?crew(58,64)+barrel(62, seventy(),40,3)+rect(41,50,10,27,wood):v===1?crew(52,64,1)+poly('63,37 78,42 83,64 78,87 63,91',steel)+rect(61,54,18,20,olive)+barrel(77,64,32,4):crew(51,64)+rect(30,46,20,36,dark,4)+circle(40,53,6,brass)+circle(40,76,6,brass)+line(44,78,75,70,wood,4)+barrel(62,66,43,8);
  if(kind==='scout') return v===0?crew(60,64,1)+poly('33,49 60,51 59,77 33,80',olive)+barrel(61,70,43,3):v===1?circle(36,64,12,dark)+circle(92,64,12,dark)+rect(38,54,47,20,steel)+crew(58,62)+circle(67,88,10,dark)+rect(51,80,27,17,olive)+line(78,49,78,77,brass,3):wheels(22,64,76,45)+poly('24,42 83,42 104,55 104,75 83,88 24,88',steel)+rect(50,45,24,40,olive)+circle(62,64,13,brass)+barrel(65,64,42,4);
  let s='';
  if(kind==='truck') {
    s=wheels(18,64,85,48)+rect(17,41,61,47,v===1?wood:olive,3)+rect(80,43,27,43,steel,5)+rect(85,47,8,34,'#b4c1b0')+rect(95,46,10,37,olive);
    if(v===0) s+=rect(21,44,53,40,'#a09172',9)+line(26,48,69,48,light)+line(26,79,69,79,wood);
    if(v===1) {for(let x=23;x<72;x+=17) for(let y=47;y<83;y+=18) s+=rect(x,y,13,14,wood)+line(x+2,y+2,x+10,y+11,brass,1);}
    if(v===2) s+=rect(18,45,58,38,steel,17)+rect(43,45,10,38,light)+circle(49,64,7,dark);
  } else if(kind==='halftrack') {
    s=(v===2?tracks(21,64,85,47):tracks(19,64,49,45)+wheels(82,64,22,45))+poly('18,40 80,39 107,48 112,64 106,86 19,89',v===1?brass:olive);
    s+=v===0?rect(24,45,52,38,dark)+rect(28,49,43,7,wood)+rect(28,74,43,6,wood)+circle(81,64,11,steel)+barrel(86,64,28,5):v===1?rect(25,44,50,41,steel)+line(30,38,30,14,steel,3)+line(24,17,74,24,brass,3)+circle(60,64,15,olive):circle(63,64,24,steel)+barrel(62,54,48,6)+barrel(62,74,48,6)+rect(31,50,14,28,wood);
  } else {
    const heavy=kind==='heavy';
    s=tracks(heavy?14:22,64,heavy?87:73,heavy?59:48)+poly(heavy?'16,34 85,29 104,43 109,64 104,86 85,99 16,94':'22,40 84,36 98,48 103,64 96,84 24,88',v===1?steel:olive);
    if(v===0) s+=poly(heavy?'43,41 72,37 91,54 91,77 72,89 43,84':'43,46 71,43 87,54 87,75 70,85 43,80',steel)+barrel(68,64,52,heavy?12:9)+circle(53,62,9,olive);
    if(v===1) s+=poly('32,39 72,36 93,49 93,81 72,92 32,90',olive)+barrel(66,64,55,heavy?19:13)+rect(31,46,12,39,wood)+bolts(48,43,32,42);
    if(v===2) s+=circle(60,64,heavy?27:22,steel)+barrel(62,53,55,8)+barrel(62,77,55,8)+rect(34,51,19,28,olive)+rect(17,32,30,10,brass)+rect(17,87,30,10,brass);
    s+=line(23,46,23,82,light,2);
  }
  return s;
}
function seventy(){return 70;}
const variants={
mg:[['Полевой расчёт','Станок на треноге и открытый бруствер.'],['ДОТ «Гранит»','Закрытая бетонная казематная позиция.'],['Спарка «Шершень»','Гусеничная платформа и два ствола.']],
rifle:[['Стрелковая ячейка','Одиночная винтовка и мешки с песком.'],['Вышка «Дозор»','Деревянный настил на раскосах и оптика.'],['Расчёт «Беркут»','Два стрелка за угловыми бронещитами.']],
mortar:[['Опорная плита','Открытая позиция на круглой плите.'],['Самоходка «Туча»','Бронированный гусеничный носитель.'],['Батарея «Гром»','Два тяжёлых ствола на колёсном лафете.']],
cannon:[['Полевое орудие','Колёсный лафет, щит и раздвижные станины.'],['САУ «Бастион»','Низкая рубка на широких гусеницах.'],['Каземат «Титан»','Бетонный форт со спаренными пушками.']],
sapper:[['Инженерный пост','Запасы мин, расчёт и противотанковый ёж.'],['Тягач «Крот»','Гусеничная машина с широким отвалом.'],['Станция «Импульс»','Радиостанция и два выносных устройства.']],
rocket:[['БМ «Катюша»','Пакет направляющих на грузовом шасси.'],['РС «Буран»','Удлинённые направляющие на гусеницах.'],['Батарея «Ураган»','Шесть направляющих на стационарной опоре.']],
infantry:[['Стрелок','Винтовка, каска и полевой ранец.'],['Щитоносец','Большой бронещит и короткое оружие.'],['Огнемётчик','Баллоны, шланг и тяжёлый огнемёт.']],
scout:[['Разведчик','Плащ-накидка и лёгкое снаряжение.'],['Моторазведка','Мотоцикл с боковой коляской.'],['Бронедозор','Четырёхколёсная разведывательная машина.']],
truck:[['Тентованный','Грузовой кузов под тканевым тентом.'],['Боеприпасы','Открытый кузов с отдельными ящиками.'],['Цистерна','Объёмный бак с люком и обвязкой.']],
halftrack:[['Полугусеничный','Открытый десантный отсек и передние колёса.'],['Командный','Закрытая радиорубка и рамочная антенна.'],['Зенитный','Полные гусеницы и спаренная установка.']],
tank:[['Средний танк','Поворотная башня и длинная пушка.'],['Штурмовое орудие','Цельная рубка и утолщённый ствол.'],['Двухствольный','Круглая башня со спаркой и боковыми блоками.']],
heavy:[['Тяжёлый танк','Широкие гусеницы и массивная башня.'],['Осадная машина','Высокая рубка и крупнокалиберное орудие.'],['Сухопутный крейсер','Два ствола и увеличенные бортовые модули.']]
};
const towers=['mg','rifle','mortar','cannon','sapper','rocket']; const models=[];
for(const [unit,entries] of Object.entries(variants)) entries.forEach(([name,description],v)=>{
  const id=unit+'_'+v; const asset='Art/Models/'+id+'.svg';
  const shape=towers.includes(unit)?tower(unit,v):enemy(unit,v);
  fs.writeFileSync(path.join(out,id+'.svg'),`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="512" height="512"><ellipse cx="65" cy="71" rx="46" ry="31" fill="#000" opacity=".24"/>${shape}</svg>\n`);
  models.push({id,unit,name,description,variant:v,price:v*6,asset});
});
fs.writeFileSync(path.join(root,'native/Game/Data/models.json'),JSON.stringify(models,null,2)+'\n');
let gallery=`<svg xmlns="http://www.w3.org/2000/svg" width="1260" height="${180+12*200}" viewBox="0 0 1260 ${180+12*200}"><rect width="100%" height="100%" fill="#101a18"/><style>text{font-family:DejaVu Sans,sans-serif;fill:#e0e4ce}.small{font-size:14px;fill:#94a795}</style><text x="50" y="65" font-size="40" letter-spacing="7">РУБЕЖ / АРСЕНАЛ</text><text x="50" y="102" class="small">36 моделей · 12 типов · различия в геометрии, корпусе и вооружении</text>`;
Object.entries(variants).forEach(([unit,entries],row)=>entries.forEach(([name,description],v)=>{
 let x=40+v*410,y=135+row*200;
 gallery+=`<rect x="${x}" y="${y}" width="392" height="185" rx="10" fill="#1c2925" stroke="#324339"/><g transform="translate(${x+8},${y+12})">${towers.includes(unit)?tower(unit,v):enemy(unit,v)}</g><text x="${x+144}" y="${y+54}" font-size="16">${name}</text><text x="${x+144}" y="${y+80}" class="small">${unit.toUpperCase()} / 0${v+1}</text><text x="${x+18}" y="${y+158}" class="small">${description}</text>`;
}));
gallery+='</svg>'; fs.mkdirSync(path.join(root,'docs'),{recursive:true}); fs.writeFileSync(path.join(root,'docs/models.svg'),gallery);
console.log(`Built ${models.length} geometry variants and docs/models.svg`);
