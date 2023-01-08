import Link from 'next/link'
import Navbar from 'react-bootstrap/Navbar'
import Nav from 'react-bootstrap/Nav'
import consts from '../consts'
import Container from 'react-bootstrap/Container'

export default function Header() {
  return (
    <>
      <Navbar bg="light" expand="sm" className='header-bg'>
      <Container>
        <Link href="/" passHref><Navbar.Brand>{consts.title}</Navbar.Brand></Link>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">
            <Link href="/top" passHref><Nav.Link>Leaderboard</Nav.Link></Link>
            <Link href="/highscores" passHref><Nav.Link>Highscores</Nav.Link></Link>
          </Nav>
        </Navbar.Collapse>
        </Container>
      </Navbar>
      <style jsx>{`
        .header-bg {
          background-color: #e3f2fd!important;
        }`}
      </style>
    </>
    
  );
}