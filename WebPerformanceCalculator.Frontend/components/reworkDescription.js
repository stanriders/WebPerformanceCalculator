
import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import Link from 'next/link'
import CurrentBuild from '../components/currentBuild'
import consts from '../consts'

export default function ReworkDescription() {
  return (
    <>
      <Card>
        <Card.Body>
          <Card.Text>
            <div dangerouslySetInnerHTML={{
                  __html: consts.description}} />
          </Card.Text>
        </Card.Body>
      </Card>
    </>
  )
}