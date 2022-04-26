import TimeAgo from 'react-timeago'
import consts from '../consts'
import api from '../lib/api'
import useSWR from 'swr'

function formatDate(date){
  const parsedDate = new Date(date);
  return `${parsedDate.toLocaleDateString()} ${parsedDate.toLocaleTimeString()}`;
}

export default function CurrentBuild( { build } ) {
  const { data, error, isValidating } = useSWR(
    '/version', api
  )

  return (
    <>
			<b>Current build:</b> {!data && isValidating && (`Loading...`)}
          {data && (<><time dateTime={data.date}>{formatDate(data.date)}</time> (<TimeAgo date={data.date}/>) | <a href={consts.link}>GitHub</a> ({data.commit})</>)}
		</>
	);
}
