import Link from 'next/link'
import Card from 'react-bootstrap/Card'
import ListGroup from 'react-bootstrap/ListGroup'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import { addToQueue } from '../lib/api'
import api from '../lib/api'
import useSWR from 'swr'

export default function Queue() {
  const { data, error, isValidating, mutate } = useSWR(
      '/queue', api
  )

  const submit = async event => {
    event.preventDefault()

    document.getElementById('error').innerHTML = "";

    const newPlayer = event.target.username.value;
    const options = {
      optimisticData: [...data, newPlayer],
      rollbackOnError: true,
      revalidate: true
    };

    try {
      await mutate(await addToQueue(newPlayer), options);
    } catch (error) {
      document.getElementById('error').innerHTML = error.err;
    }

    event.target.reset();
  }

  return (
    <>
      <Card>
        <Card.Body>
          <Card.Title>Queue</Card.Title>
          <Card.Text>
            <p>Try searching the leaderboard before adding someone to the calculation queue.</p>

            <Form onSubmit={submit}>
              <Form.Group className="mb-3">
                <Form.Control id="username" name="username" placeholder="Username or user ID (preferable)" required type="text"/>
                <Form.Text className="text-muted">
                {data && (<>Estimated wait time is ~{(data.length + 1) * 2.0} seconds.</>)}
                </Form.Text>
                <Form.Text className="text-danger" id="error" >{error && error.err}</Form.Text>
              </Form.Group>
              <Button variant="secondary" type="submit">Calculate</Button>
            </Form>
          </Card.Text>
          <div className="queue">
            <ListGroup>
              {!data && isValidating && (
                <p>Loading...</p>
              )}
              {data && data.length > 0 && data.map((queue) => (
                <ListGroup.Item>{queue}</ListGroup.Item>
              ))}
            </ListGroup>
          </div>
        </Card.Body>
      </Card>

      <style jsx>{`
        .queue {
          max-height: 10vh;
          overflow: auto;
        }`}
      </style>
    </>
    );
}