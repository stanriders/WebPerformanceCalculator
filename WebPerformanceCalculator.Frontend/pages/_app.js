import Layout from '../components/layout'
import SSRProvider from 'react-bootstrap/SSRProvider';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';

export default function MyApp({ Component, pageProps }) {
  return (
    <SSRProvider>
    <Layout>
      <Component {...pageProps} />
    </Layout>
    </SSRProvider>
  )
}