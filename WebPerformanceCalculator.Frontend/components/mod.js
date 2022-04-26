
import Badge from 'react-bootstrap/Badge'

export default function Mod({name}) {
  return (
    <>
      <Badge bg="light" className="mod">{name}</Badge>

      <style jsx>{`
        .mod {
          margin: 0 5px;
        }`}
      </style>
    </>
    );
}