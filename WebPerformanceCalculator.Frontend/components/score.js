import Link from 'next/link'
import Mod from '../components/mod'

export default function Score({data}) {
  return (
    <>
      <Link href={`/map/${data.map.id}`}><a>{data.map.name}</a></Link>{' '}
      {data.mods.split(', ').sort().map((item) => (<Mod name={item} key={item}/> ))}{' '}
      <span className="scoreData">({data.accuracy.toFixed(2)}%, {data.combo}x{data.misses > 0 && `, ${data.misses} misses`})</span>

      <style jsx>{`
        .scoreData {
          font-size: 11px
        }`}
      </style>
    </>
    );
}