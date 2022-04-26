import Head from 'next/head'
import Link from 'next/link'
import Table from 'react-bootstrap/Table'
import Pagination from 'react-bootstrap/Pagination'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import InputGroup from 'react-bootstrap/InputGroup'
import FormControl from 'react-bootstrap/FormControl'
import Button from 'react-bootstrap/Button'
import api from '../lib/api'
import Player from '../components/player'
import Rank from '../components/rank'
import consts from '../consts'
import { useRouter } from 'next/router'
import { useEffect, useState } from 'react'
import useSWR from 'swr'

export default function Top() {
  const router = useRouter()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const {
    data: players,
    error: playersError,
    isValidating: playersValidating } = useSWR(
      `/top?offset=${(page-1) * 50}&limit=${50}&search=${search}`, api
  )

  const totalPages = players && Math.ceil(players.total / 50);

  // Handle direct link to page and/or filter
  useEffect(() => {
    const qs = new URLSearchParams(window.location.search)
    const qsPage = Number(qs.get('page'))
    const qsSearch = qs.get('search')

    if (qsPage > 0) {
      setPage(qsPage)
    }

    if (qsSearch) {
      setSearch(qsSearch)
    }
  }, [])

  // Update history and url when filter/page changes
  useEffect(() => {
    router.push({
      query: {
        page,
        search
      },
    }, undefined, {
      scroll: false,
    })
  }, [page, search])


  function handlePrevClick() {
    if (page > 1) {
      setPage(page - 1)
    }
  }

  return (
    <>
      <Head>
        <title>Top - {consts.title}</title>
      </Head>

      <Form className="mt-2" onSubmit={(e) => {e.preventDefault(); setSearch(e.target.username.value)}}>
        <Row>
          <Col xs="auto">
            <InputGroup className="mb-2">
              <FormControl id="username" placeholder="Username" />
            </InputGroup>
          </Col>
          <Col xs="auto">
            <Button type="submit" variant="secondary">Search</Button>
          </Col>
        </Row>
      </Form>

      <Table striped bordered hover size="sm">
        <thead>
          <tr><td className="rank">#</td><td>Username</td><td className="pp">PP</td></tr>
        </thead>
        <tbody>
          {!players && playersValidating && (<tr><td colSpan="3" className="loading">Loading...</td></tr>)}
          {!players && playersError && playersError.info && (<tr><td colSpan="3" className="loading">{playersError.info}</td></tr>)}
          {players && players.rows.length === 0 && (<tr><td colSpan="3" className="loading">No players!</td></tr>)}
          {players && players.rows.map((data) => (
              <tr key={data.id}>
                <td className="rank">
                  <Rank rank={data.place} rankChange={-data.rankChange} />
                </td>
                <td>
                  <Player id={data.id} username={data.name} country={data.country}/>
                </td>
                <td className="pp">
                  {data.localPp.toFixed(2)}
                </td>
              </tr>
          ))}
        </tbody>
      </Table>

      {players && (
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
          width: 96px;
          text-align: center;
        }
        .pp {
          width: 96px;
          text-align: center;
        }`}
      </style>
    </>
  )
}

