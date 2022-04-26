import Head from 'next/head'
import { getMap, updateMap } from '../../lib/api'
import consts from '../../consts'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

export default function Map({mapData}) {
    const submit = async event => {
        event.preventDefault()

        try{
            await updateMap(mapData.id, Number(event.target.percentage.value));
            history.back();
        } catch (e) {
            document.getElementById('error').innerHTML = JSON.stringify(e);
        }
      }

  return (
    <>
      <Head>
        <title>{mapData.name} - {consts.title}</title>
      </Head>

      <Card>
        <Card.Body>
          <Card.Title>{mapData.name}</Card.Title>
          <Card.Text>
            <p>Keep in mind that updating percentage does NOT update it immediately for every score - people will have to recalculate their profile first!</p>
            <p>This is <b>final</b> percentage, no additional bonuses apply after it!</p>
            <Form onSubmit={submit}>
              <Form.Group className="mb-3">
                <Form.Control id="percentage" name="percentage" placeholder="Adjustment Percentage" required type="number" step="0.000000000000001" min="0.7" max="1.3" defaultValue={mapData.adjustmentPercentage}/>
                <Form.Text className="text-danger" id="error" ></Form.Text>
              </Form.Group>
              <Button variant="secondary" type="submit">Update</Button>
            </Form>
          </Card.Text>
        </Card.Body>
      </Card>
    </>
  );
}

export async function getServerSideProps(context) {
  const mapData = await getMap(context.params.id);
  return {
    props: {mapData}
  }
}
