const moment = require('moment-timezone');


//memomet tester
// 2025年4月8日を基準日として使用（現在の日付）
const referenceDate = moment.utc("2025-11-08");

// すべてのタイムゾーン名を取得
const timeZones = moment.tz.names();


function stringifyWithInfinity(obj) {
	return JSON.stringify(obj, (key, value) => {
		if (value === Infinity) {
			return 'Infinity';
		} else if (value === -Infinity) {
			return '-Infinity';
		}
		return value;
	});
}

function infinytime_keep(date_until) {
	if (date_until == "Infinity") {
		return "INFINTY";
	}

	var date = moment.utc(date_until).format();

	return date;
}

function infinytime_keep_tz(date_until, tzst) {
	if (date_until == "Infinity") {
		return "INFINTY";
	}

	var date = moment(date_until).tz(tzst).format();

	return date;
}

// 各タイムゾーンのオフセット情報を表示
	var header = "OS時間</th><th>TZローカル版</th><th>タイムゾーン</th>";
  console.log(header);
  var st="";
  
timeZones.forEach((tzName) => {
  const tt = moment.tz.zone(tzName);
  const utcMoment = moment.utc(referenceDate);
  const localMoment = utcMoment.clone().tz(tzName);

  for (var i = 0; i < tt.abbrs.length; i++) {
    if(moment(tt.untils[i]).format("YYYY") == "2025"){
		st += tzName+","+infinytime_keep(tt.untils[i]) +","  + infinytime_keep_tz(tt.untils[i], tzName) +"\r\n";
	}}

  console.log(st);
  st="";
});

