import Head from 'next/head'
import Table from 'react-bootstrap/Table'
import Pagination from 'react-bootstrap/Pagination'
import Player from '../components/player'
import Score from '../components/score'
import api from '../lib/api'
import consts from '../consts'
import { useRouter } from 'next/router'
import { Fragment, useEffect, useRef, useState } from 'react'
import useSWR from 'swr'

export default function Highscores() {
  const router = useRouter()
  const [page, setPage] = useState(1)
  const offset = (page-1) * 50;

  const {
    data: scores,
    error: scoresError,
    isValidating: scoresValidating } = useSWR(
      `/highscores?offset=${offset}&limit=${50}`, api
  )

  const totalPages = scores && Math.ceil(scores.total / 50);

  // Handle direct link to page and/or filter
  useEffect(() => {
    const qs = new URLSearchParams(window.location.search)
    const qsPage = Number(qs.get('page'))

    if (qsPage > 0) {
      setPage(qsPage)
    }
  }, [])

  // Update history and url when filter/page changes
  useEffect(() => {
    router.push({
      query: {
        page
      },
    }, undefined, {
      scroll: false,
    })
  }, [page])


  function handlePrevClick() {
    if (page > 1) {
      setPage(page - 1)
    }
  }

  return (
    <>
      <Head>
        <title>Highscores - {consts.title}</title>
      </Head>

      <Table striped bordered hover size="sm">
        <thead>
          <tr><td className="rank">#</td><td className="player">Player</td><td>Beatmap</td><td className="pp">PP</td></tr>
        </thead>
        <tbody>
          {!scores && scoresValidating && (<tr><td colSpan="4" className="loading">Loading...</td></tr>)}
          {!scores && scoresError && scoresError.info && (<tr><td colSpan="4" className="loading">{scoresError.info}</td></tr>)}
          {scores && scores.rows.length === 0 && (<tr><td colSpan="4" className="loading">No highscores!</td></tr>)}
          {scores && scores.rows.map((data, index) => (
              <tr key={data.position}>
                <td className="rank">
                  {offset + index + 1}
                </td>
                <td className="player">
                  <Player id={data.player.id} username={data.player.name} country={data.player.country}/>
                </td>
                <td>
                  <Score data={data}/>
                </td>
                <td className="pp">
                  {data.localPp.toFixed(2)}
                </td>
              </tr>
          ))}
        </tbody>
      </Table>

      {scores && (
        <Pagination>
          <Pagination.First onClick={() => setPage(1)} disabled={page <= 1} />
          <Pagination.Prev onClick={handlePrevClick} disabled={page <= 1} />

          {page > 2 && (<Pagination.Item onClick={() => setPage(page -2)}>{page - 2}</Pagination.Item>)}
          {page > 1 && (<Pagination.Item onClick={handlePrevClick}>{page - 1}</Pagination.Item>)}
          <Pagination.Item active activeLabel="">{page}</Pagination.Item>
          {page < totalPages && (<Pagination.Item onClick={() => setPage(page + 1)}>{page+1}</Pagination.Item>)}
          {page < totalPages - 1 && (<Pagination.Item onClick={() => setPage(page + 2)}>{page+2}</Pagination.Item>)}

          <Pagination.Next onClick={() => setPage(page + 1)} disabled={page >= totalPages} />
          <Pagination.Last onClick={() => setPage(totalPages)} disabled={page >= totalPages} />
        </Pagination>
      )}

      <style jsx>{`
        .loading {
          width: 100%;
          text-align: center;
        }
        .rank {
          width: 48px;
          text-align: center;
        }
        .player {
          width: 180px;
        }
        .pp {
          width: 96px;
          text-align: center;
        }`}
      </style>
    </>
  )
}