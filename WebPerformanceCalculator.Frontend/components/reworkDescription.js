
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
            <p><CurrentBuild /></p>
            <div dangerouslySetInnerHTML={{
                  __html: consts.description}} />
            <Alert variant="warning">
              Please give the feedback about this rework through <b><Link href="https://forms.gle/qQMCscj6vYVc6uVh6">this form</Link></b>, we would really appreciate it!
            </Alert>
          </Card.Text>
        </Card.Body>
      </Card>
    </>
  )
}