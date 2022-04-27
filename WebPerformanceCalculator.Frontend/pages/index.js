import Head from 'next/head'
import Image from 'next/image'
import ReworkDescription from '../components/reworkDescription'
import Queue from '../components/queue'
import consts from '../consts'
import Card from 'react-bootstrap/Card'

export default function Home({build, queue}) {
  return (
    <>
      <Head>
        <title>{consts.title}</title>
      </Head>
      <ReworkDescription />
      <hr />
      {/*<Queue />*/}
      <Card>
        <Card.Body>
        <Card.Title>The experiment has concluded.</Card.Title>
          <Card.Text>
            <p>Congratulations osu! community for not (completely) falling for it!</p>
            <p>As many of you thought, this "rework" is indeed mostly random.<br />
            It's applying a random multiplier to the map between 90% and 110% and then multiplies it by an <a href="https://www.desmos.com/calculator/ff8krhpr4d">old map bonus</a> (the lower the map id - the bigger the bonus).</p>
            <p><a href="https://docs.google.com/spreadsheets/d/1KzfojxMMiPnPXhvTuYQB9OqK4EiS1ZPvFVSAZa4gVoE/edit?usp=sharing">Results</a> of the survey: </p>
            <Card.Img as={Image} src="/results.png" width="440" height="222" />
            <p>Some of you were really passionate about it, and we appreciate it and hope you can provide the same level and amount of feedback on the <a href="https://discord.gg/aqPCnXu">PP Development Server</a> for other (real!) reworks that are in the development!</p>
            <p>See you next time!</p>
          </Card.Text>
        </Card.Body>
      </Card>
      </>
  );
}