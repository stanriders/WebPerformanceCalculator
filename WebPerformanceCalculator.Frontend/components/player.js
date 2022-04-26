import Link from 'next/link'

export default function Player({id, username, country, outLink}) {
  return (
    <>
      <Link href={`/countrytop/${country}`}><a><img className="flag" src={flagUrl(country)}/></a></Link>
      <Link href={outLink ? 
        `https://osu.ppy.sh/users/${id}` :
        `/users/${id}`}>
      <a>{username}</a></Link>
      <style jsx>{`
        .flag {
          width: 32px; 
          padding: 0 5px;
        }`}
      </style>
    </>
    );
}

function flagUrl(code) {
  if (!!code)
  {
    var flagName = code.split('')
      .map(function(c) { return (c.charCodeAt(0) + 127397).toString(16)})
      .join('-');

    return `https://osu.ppy.sh/assets/images/flags/${flagName}.svg`;
  }
  return '';
};