import Head from 'next/head'
import consts from '../consts'
import Table from 'react-bootstrap/Table'

export default function Weights() {
    return (
    <>
        <Head>
          <title>Weights - {consts.title}</title>
        </Head>

        <Table striped bordered size="sm">
            <thead>
            <tr><td>Mapper</td><td className="weight">Weight</td></tr>
            </thead>
            <tbody>
                <tr>
                    <td>Sotarks</td>
                    <th className="weight" rowSpan="16">
                        0.7
                    </th>
                </tr>
                <tr><td>fieryrage</td></tr>
                <tr><td>nevo</td></tr>
                <tr><td>fatfan kolek</td></tr>
                <tr><td>taeyang</td></tr>
                <tr><td>reform</td></tr>
                <tr><td>armin</td></tr>
                <tr><td>bibbity bill</td></tr>
                <tr><td>log off now</td></tr>
                <tr><td>azu</td></tr>
                <tr><td>dendyhere</td></tr>
                <tr><td>browiec</td></tr>
                <tr><td>emu1337</td></tr>
                <tr><td>onlybiscuit</td></tr>
                <tr><td>kowari</td></tr>
                <tr><td>DeRandom Otaku</td></tr>
                <tr></tr>
                <tr>
                    <td>seni</td>
                    <th className="weight" rowSpan="12">
                        0.85
                    </th>
                </tr>
                <tr><td>monstrata</td></tr>
                <tr><td>snownino_</td></tr>
                <tr><td>xexxar</td></tr>
                <tr><td>lami</td></tr>
                <tr><td>akitoshi</td></tr>
                <tr><td>doormat</td></tr>
                <tr><td>kencho</td></tr>
                <tr><td>skyflame</td></tr>
                <tr><td>Kuki1537</td></tr>
                <tr><td>Kagetsu</td></tr>
                <tr><td>Shmiklak</td></tr>
            </tbody>
        </Table>
        <style jsx>{`
            .weight {
            width: 96px;
            text-align: center;
            vertical-align: middle;
            }`}
        </style>
    </>
    );
  }