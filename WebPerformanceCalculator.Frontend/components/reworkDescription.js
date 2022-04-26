
import Card from 'react-bootstrap/Card'
import CurrentBuild from '../components/currentBuild'
import consts from '../consts'

export default function ReworkDescription() {
  return (
    <>
      <Card>
        <Card.Body>
          <Card.Text>
            <p><CurrentBuild /></p>
            <div dangerouslySetInnerHTML={{
                  __html: consts.description}} /></Card.Text>
        </Card.Body>
      </Card>
    </>
  )
}