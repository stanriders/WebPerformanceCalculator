import Head from 'next/head'
import { getPlayer } from '../../lib/api'
import Score from '../../components/score'
import Player from '../../components/player'
import Rank from '../../components/rank'
import Table from 'react-bootstrap/Table'
import TimeAgo from 'react-timeago'
import consts from '../../consts'

function formatDate(date){
  const parsedDate = new Date(date);
  return `${parsedDate.toLocaleDateString()} ${parsedDate.toLocaleTimeString()}`;
}

export default function User({playerData}) {
  return (
    <>
      <Head>
        <title>{playerData.name} - {consts.title}</title>
      </Head>

      <h1><Player id={playerData.id} username={playerData.name} country={playerData.country} outLink="true"/></h1>
      <div><b>Updated on:</b> <time dateTime={playerData.updateTime}>{formatDate(playerData.updateTime)}</time></div>
      <div><b>Total PP:</b> {playerData.localPp.toFixed(2)}</div>
      <br/>
      <Table striped bordered hover size="sm">
        <thead>
          <tr>
            <td className="rank">#</td>
            <td>Beatmap</td>
            <td className="pp">Live PP</td>
            <td className="pp">PP</td>
            <td className="diff">Difference</td>
          </tr>
        </thead>
        <tbody>
          {playerData.scores.map((data, index) => (
              <tr key={data.map.id}>
                <td className="rank">
                  <Rank rank={index + 1} rankChange={data.positionChange} />
                </td>
                <td>
                  <Score data={data}/>
                </td>
                <td className="pp">
                  {data.livePp.toFixed(2)}
                </td>
                <td className="pp">
                  <b>{data.localPp.toFixed(2)}</b> <span className="weighted">({(Math.pow(0.95, index) * data.localPp).toFixed(1)})</span>
                </td>
                <td className="diff">
                  {(data.localPp - data.livePp).toFixed(2)}
                </td>
              </tr>
          ))}
        </tbody>
      </Table>
      <style jsx>{`
        .rank {
          width: 96px;
          text-align: center;
        }
        .pp {
          width: 128px;
          text-align: center;
        }
        .diff {
          width: 96px;
          text-align: center;
        }
        .weighted {
          font-size: 9px;
        }`}
      </style>
    </>
  );
}

export async function getServerSideProps(context) {
  const playerData = await getPlayer(context.params.id);
  return {
    props: {playerData}
  }
}
