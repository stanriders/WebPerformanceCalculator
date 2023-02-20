import Head from 'next/head'
import ReworkDescription from '../components/reworkDescription'
import Queue from '../components/queue'
import consts from '../consts'

export default function Home({build, queue}) {
  return (
    <>
      <Head>
        <title>{consts.title}</title>
      </Head>
      <ReworkDescription />
      <hr />
      <Queue />
      </>
  );
}