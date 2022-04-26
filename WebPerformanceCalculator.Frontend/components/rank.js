
import Badge from 'react-bootstrap/Badge'

export default function Rank({rank, rankChange}) {
  return (
    <>{rank}{' '}
      {rankChange != 0 && (rankChange < 0 ?
        <>(<span className="rankDown">▾{Math.abs(rankChange)}</span> )</> :
        <>(<span className="rankUp">▴{rankChange}</span> )</>)
      }

      <style jsx>{`
        .rankUp {
          color: limegreen;
        }
        .rankDown {
          color: red;
        }`}
      </style>
    </>
    );
}