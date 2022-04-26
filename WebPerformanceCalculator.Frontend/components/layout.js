import Head from 'next/head'
import Link from 'next/link'
import Header from './header'
import Container from 'react-bootstrap/Container'

export default function Layout({ children }) {
  return (
    <>
      <Container>
        <main role="main" className="pb-1 pt-2">
          <Header />
          {children}
        </main>
      </Container>
    </>
  )
}